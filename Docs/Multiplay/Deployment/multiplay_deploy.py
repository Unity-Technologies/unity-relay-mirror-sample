#!/usr/bin/env python3

import argparse
import json
import os
import requests
from requests_aws4auth import AWS4Auth
import subprocess
import time
import urllib

###
### These constants can be found in the Service Identifiers document you received during your onboarding
###
MULTIPLAY_HOST = 'api.multiplay.co.uk'
MULTIPLAY_REGION = 'eu-west-1'
MULTIPLAY_SERVICE = 'cf' # cf is short for Clanforge
MULTIPLAY_API_HEADERS = {'Content-Type': 'application/x-www-form-urlencoded'}

JOB_STATE_CREATED = 1
JOB_STATE_PENDING = 2
JOB_STATE_QUEUED = 3
JOB_STATE_RUNNING = 4
JOB_STATE_COMPLETE = 5
JOB_STATE_FAILED = 6
JOB_STATE_SQUASHED = 7

parser = argparse.ArgumentParser(description = 'Deploy your dedicated game server to Multiplay.')
parser.add_argument('--secret-name', action='store', type=str, help='The AWS Secrets Manager secret name containing credentials.')
parser.add_argument('--full-deploy', action='store_true', help='Whether to manually create a full image. It is best practive to occasionally perform a full deploy.')

required_group = parser.add_argument_group('required arguments')
required_group.add_argument('--server-path', action='store', type=str, help='The filepath on your machine for the dedicated server.', required=True)
required_group.add_argument('--version', action='store', type=str, help='The version of the game server.', required=True)
required_group.add_argument('--s3-bucket-name', action='store', type=str, help='The name of the S3 bucket used to host the dedicated server files.', required=True)
required_group.add_argument('--image', action='store', type=str, help='The image you would like to deploy to.', choices=['a', 'b'], required=True)

args = parser.parse_args()

def main():
    s3_bucket_path = 's3://{s3_bucket_name}/dedicated-server-{version}'.format(
        s3_bucket_name=args.s3_bucket_name,
        version=args.version)

    ###
    ### We don't want to store sensitive credentials in this script.
    ### We could pass them in via commandline arguments or set them as environment variables.
    ### Another option is retrieving them from an external secret store.
    ###
    print('Retrieving secrets...')
    aws_access_key = ''
    aws_secret_access_key = ''

    ###
    ### The following secrets can be found in the Service Identifiers document you received during your onboarding
    ###
    multiplay_access_key = ''
    multiplay_secret_access_key = ''
    account_service_id = ''
    build_machine_id = ''
    image_a_id = ''
    image_b_id = ''

    if args.secret_name:
        multiplay_credentials_output = subprocess.check_output(['aws', 'secretsmanager', 'get-secret-value', '--secret-id', args.secret_name, '--output', 'json', '--query', 'SecretString'])
        multiplay_credentials_output_json = json.loads(json.loads(multiplay_credentials_output.decode('utf-8')))

        aws_access_key = multiplay_credentials_output_json.get('aws_access_key')
        aws_secret_access_key = multiplay_credentials_output_json.get('aws_secret_access_key')
        multiplay_access_key = multiplay_credentials_output_json.get('multiplay_access_key')
        multiplay_secret_access_key = multiplay_credentials_output_json.get('multiplay_secret_access_key')
        account_service_id = multiplay_credentials_output_json.get('account_service_id')
        build_machine_id = multiplay_credentials_output_json.get('build_machine_id')
        image_a_id = multiplay_credentials_output_json.get('image_a_id')
        image_b_id = multiplay_credentials_output_json.get('image_b_id')
    else:
        aws_access_key = os.getenv('AWS_ACCESS_KEY')
        aws_secret_access_key = os.getenv('AWS_SECRET_ACCESS_KEY')
        multiplay_access_key = os.getenv('MULTIPLAY_ACCESS_KEY')
        multiplay_secret_access_key = os.getenv('MULTIPLAY_SECRET_ACCESS_KEY')
        account_service_id = os.getenv('ACCOUNT_SERVICE_ID')
        build_machine_id = os.getenv('BUILD_MACHINE_ID')
        image_a_id = os.getenv('IMAGE_A_ID')
        image_b_id = os.getenv('IMAGE_B_ID')
    
    ###
    ### Determine which image we intend to deploy to. You may only receive one image when you are first onboarded.
    ### If that is the case, you will always want to deploy to that image. Multiple images become necessary when you begin
    ### to implement zero downtime deployment.
    ###
    selected_image_id = image_a_id if args.image == 'a' else image_b_id

    print('Deploying dedicated server version "{version}" from "{server_path}" to image "{image_id}". Intermediate storage at: "{s3_bucket_path}"'.format(
        version=args.version,
        server_path=args.server_path,
        image_id=selected_image_id,
        s3_bucket_path=s3_bucket_path))

    ###
    ### First, we upload our game server to S3 for intermediate storage.
    ### Multiplay will download these files to your build machine.
    ###
    subprocess.check_call(['aws', 's3', 'sync', args.server_path, s3_bucket_path, '--no-progress', '--only-show-errors'])

    ###
    ### In order for Multiplay to download our game server, we need a URL that contains credentials.
    ### Note: Temporary credentials retrieved from STS are not supported.
    ###
    authenticated_s3_bucket_path = s3_bucket_path.replace('s3://', 's3://{aws_access_key}:{aws_secret_access_key}@'.format(
        aws_access_key=urllib.parse.quote(aws_access_key, safe=''),
        aws_secret_access_key=urllib.parse.quote(aws_secret_access_key, safe='')))

    ###
    ### Next we need to construct our credentials to use against Multiplay's API.
    ### Multiplay uses AWS Signature 4 authentication.
    ### More information can be found here: https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html
    ###
    auth = AWS4Auth(multiplay_access_key, multiplay_secret_access_key, MULTIPLAY_REGION, MULTIPLAY_SERVICE)

    ###
    ### We're now ready to kick off the image update process. The first step is to request the creation of an image update job.
    ### This will instruct Multiplay to download the game server from our S3 bucket to your build machine.
    ### https://docs.multiplay.com/game_images/image_update_create/
    ###
    image_update_job = create_image_update_job(account_service_id, build_machine_id, selected_image_id, args.version, authenticated_s3_bucket_path, auth)

    ###
    ### Now that we've requested an image update job to be created, we need to poll it's status.
    ### The status is contained in the response's 'jobstateid' field.
    ### Response field documentation: https://docs.multiplay.com/game_images/job_fields/
    ### 'jobstateid' definitions: https://docs.multiplay.com/game_images/job_state_definitions/
    ### Image Update Status documentation: https://docs.multiplay.com/game_images/image_update_status/
    ###
    image_update_job = poll_image_update_job(account_service_id, image_update_job, auth)

    ###
    ### Before continuing, we need to ensure the job was successful.
    ### We will raise an exception if anything goes wrong, unless the image is locked by another diff.
    ## In this case, we will reject the existing diff and allow an opportunity to retry.
    ###
    image_update_job_success = check_image_update_job_success(account_service_id, image_update_job, auth)

    ###
    ### If we've made it this far without raising an exception and the job failed, we can safely retry.
    ### We can now retry the image update.
    ### https://docs.multiplay.com/game_images/image_update_retry/
    ###
    if not image_update_job_success:
        image_update_job = create_image_update_job(account_service_id, build_machine_id, selected_image_id, args.version, authenticated_s3_bucket_path, auth)
        image_update_job = poll_image_update_job(account_service_id, image_update_job, auth)
        image_update_job_success = check_image_update_job_success(account_service_id, image_update_job, auth)
        if not image_update_job_success:
            raise Exception('Image update job failed')

    print('Completed Image Update Job: {}\n'.format(image_update_job))

    ###
    ### Now that we've finished creating an image update job, we need to create a diff.
    ### This will diff the game server that is currently on the image with what was downloaded from our S3 bucket.
    ### https://docs.multiplay.com/game_images/image_diff_create/
    ###
    image_diff_job = create_image_diff_job(account_service_id, build_machine_id, selected_image_id, auth)
    diff_id = image_diff_job.get('diffid')

    ###
    ### Now that we've created an image diff job, we need to poll it's status.
    ### Much like the previous step, the status is contained in the response's 'jobstateid' field.
    ### Response field documentation: https://docs.multiplay.com/game_images/job_fields/
    ### 'jobstateid' definitions: https://docs.multiplay.com/game_images/job_state_definitions/
    ### Image Diff Status documentation: https://docs.multiplay.com/game_images/image_diff_status/
    ###
    image_diff_job = poll_image_diff_job(account_service_id, image_diff_job, diff_id, auth)

    print('Completed Image Diff Job: {}\n'.format(image_diff_job))

    ###
    ### Before creating a new version of the image, we must determine if the diff contains any changes.
    ### If it does not, we need create a full new version in the next step.
    ### If it does, we will create a patch in the next step.
    ###
    diffs_present = False
    for path, data in sorted(image_diff_job.get('added', {}).items()):
        print('+ {}'.format(path))
        diffs_present = True
    for path, data in sorted(image_diff_job.get('modified', {}).items()):
        print('* {}'.format(path))
        diffs_present = True
    for path, data in sorted(image_diff_job.get('removed', {}).items()):
        print('- {}'.format(path))
        diffs_present = True

    ###
    ### Once a diff exists, we need to create a new version of the image.
    ### https://docs.multiplay.com/game_images/image_version_create/
    ###
    image_version_job = create_image_version_job(account_service_id, diff_id, diffs_present, args.full_deploy, args.version, auth)
    image_version_id = image_version_job.get('imageversionid')

    ###
    ### Now that we've created an image version job, we need to poll it's status.
    ### Much like the previous step, the status is contained in the response's 'jobstateid' field.
    ### Response field documentation: https://docs.multiplay.com/game_images/job_fields/
    ### 'jobstateid' definitions: https://docs.multiplay.com/game_images/job_state_definitions/
    ### Image Version Status documentation: https://docs.multiplay.com/game_images/image_version_status/
    ###
    image_version_job = poll_image_version_job(account_service_id, image_version_job, image_version_id, auth)

    print('Completed Image Version Job: {}'.format(image_version_job))
        
    ###
    ### The last thing we need to do is ensure that the new version of the image is installed on all machines.
    ### https://docs.multiplay.com/game_images/image_install_status/
    ###
    poll_image_install_status(account_service_id, auth)

    line_break = '################'
    print('\n{line_break}\n{line_break}\nSuccessfully Completed Deploy\n{line_break}\n{line_break}'.format(line_break=line_break))
    exit(0)

def create_image_update_job(account_service_id, build_machine_id, selected_image_id, version, authenticated_s3_bucket_path, auth):
    print('Creating image update job...')
    response = requests.get(
        'https://{}/cfp/v1/imageupdate/create'.format(MULTIPLAY_HOST),
        params = {
            'imageid': selected_image_id,
            'desc': version,
            'machineid': build_machine_id,
            'accountserviceid': account_service_id,
            'url': authenticated_s3_bucket_path
        },
        auth = auth,
        headers = MULTIPLAY_API_HEADERS
    )
    response.raise_for_status() # TODO: manually check for expected response

    return json.loads(response.content.decode('utf-8'))

def poll_image_update_job(account_service_id, image_update_job, auth):
    while image_update_job.get('jobstateid') in (JOB_STATE_CREATED, JOB_STATE_PENDING, JOB_STATE_QUEUED, JOB_STATE_RUNNING):
        response = requests.get(
            'https://{multiplay_host}/cfp/v1/imageupdate/{update_id}/status'.format(multiplay_host=MULTIPLAY_HOST, update_id=image_update_job.get('updateid')),
            params = {
                'accountserviceid': account_service_id
            },
            auth = auth,
            headers = MULTIPLAY_API_HEADERS
        )
        response.raise_for_status() # TODO: manually check for expected response

        image_update_job = json.loads(response.content.decode('utf-8'))
        print('{job_progress}% {job_state_name}'.format(job_progress=image_update_job.get('jobprogress'), job_state_name=image_update_job.get('jobstatename')))
        time.sleep(1.0)

    return image_update_job

def check_image_update_job_success(account_service_id, image_update_job, auth):
    if image_update_job.get('success'):
        return True
    else:
        if image_update_job.get('joberror'):
            raise Exception('Image Update Job Failed: {job_error}.\n{job}'.format(job_error=image_update_job.get('joberror'), job=image_update_job))

        # For our purposes, if a previous diff was started we want to stomp over it
        # In your own deploy process, you may want to cancel the current deploy and move forward with the pending diff
        # {'messages': [], 'error_code': 1, 'error_message': 'image locked by diffid: 218595', '_debug': [], 'success': False, 'error': True}
        beginning_of_error = 'image locked by diffid: '
        error_message = image_update_job.get('error_message')
        if beginning_of_error in error_message:
            locked_diffid = error_message.strip(beginning_of_error)
            print('Image Update Job Failed: {}. Rejecting diff'.format(error_message))

            # https://docs.multiplay.com/game_images/image_diff_reject/
            image_diff_reject_response = requests.get(
                'https://{multiplay_host}/cfp/v1/imagediff/{diffid}/reject'.format(multiplay_host=MULTIPLAY_HOST, diffid=locked_diffid),
                params = {
                    'accountserviceid': account_service_id,
                    'diffid': locked_diffid,
                    'reinstall': '0'
                },
                auth = auth,
                headers = MULTIPLAY_API_HEADERS
            )
            image_diff_reject_response.raise_for_status() # TODO: manually check for expected response

            print('Image Diff Rejected: {}'.format(image_diff_reject_response.content.decode('utf-8')))
        else:
            raise Exception('Image Update Job Failed: {error_message}.\n{job}'.format(error_message=error_message, job=image_update_job))

        return False

def create_image_diff_job(account_service_id, build_machine_id, selected_image_id, auth):
    print('Creating image diff job...')
    response = requests.get(
        'https://{}/cfp/v1/imagediff/create'.format(MULTIPLAY_HOST),
        params = {
            'accountserviceid': account_service_id,
            'imageid': selected_image_id,
            'machineid': build_machine_id
        },
        auth = auth,
        headers = MULTIPLAY_API_HEADERS
    )
    response.raise_for_status() # TODO: manually check for expected response

    return json.loads(response.content.decode('utf-8'))

def poll_image_diff_job(account_service_id, image_diff_job, diff_id, auth):
    while image_diff_job.get('jobstateid') in (JOB_STATE_CREATED, JOB_STATE_PENDING, JOB_STATE_QUEUED, JOB_STATE_RUNNING):
        response = requests.get(
            'https://{multiplay_host}/cfp/v1/imagediff/{diff_id}/status'.format(multiplay_host=MULTIPLAY_HOST, diff_id=diff_id),
            params = {
                'accountserviceid': account_service_id
            },
            auth = auth,
            headers = MULTIPLAY_API_HEADERS
        )
        response.raise_for_status() # TODO: manually check for expected response

        image_diff_job = json.loads(response.content.decode('utf-8'))
        print('{job_progress}% {job_state_name}'.format(job_progress=image_diff_job.get('jobprogress'), job_state_name=image_diff_job.get('jobstatename')))
        time.sleep(1.0)

    if not image_diff_job.get('success'):
        raise Exception('Image diff job failed: {job_error}\n{job}'.format(job_error=image_diff_job.get('joberror'), job=image_diff_job))

    return image_diff_job

def create_image_version_job(account_service_id, diff_id, diffs_present, full_deploy, version, auth):
    print('Creating image version job...')
    response = requests.get(
        'https://{}/cfp/v1/imageversion/create'.format(MULTIPLAY_HOST),
        params = {
            'diffid': diff_id,
            'restart': 1,
            'accountserviceid': account_service_id,
            'install_at': 'NOW()',
            'full': 1 if full_deploy or not diffs_present else 0,
            'game_build': version,
            'force': 1 # we don't care about existing allocations in development
        },
        auth = auth,
        headers = MULTIPLAY_API_HEADERS
    )
    response.raise_for_status() # TODO: manually check for expected response

    return json.loads(response.content.decode('utf-8'))

def poll_image_version_job(account_service_id, image_version_job, image_version_id, auth):
    while image_version_job.get('jobstateid') in (JOB_STATE_CREATED, JOB_STATE_PENDING, JOB_STATE_QUEUED, JOB_STATE_RUNNING):
        response = requests.get(
            'https://{multiplay_host}/cfp/v1/imageversion/{image_version_id}/status'.format(multiplay_host=MULTIPLAY_HOST, image_version_id=image_version_id),
            params = {
                'accountserviceid': account_service_id
            },
            auth = auth,
            headers = MULTIPLAY_API_HEADERS
        )
        response.raise_for_status() # TODO: manually check for expected response

        image_version_job = json.loads(response.content.decode('utf-8'))
        print('{job_progress}% {job_state_name}'.format(job_progress=image_version_job.get('jobprogress'), job_state_name=image_version_job.get('jobstatename')))
        time.sleep(1.0)

    if not image_version_job.get('success'):
        raise Exception('Image version job failed: {job_error}\n{job}'.format(job_error=image_version_job.get('joberror'), job=image_version_job))

    return image_version_job

def poll_image_install_status(account_service_id, auth):
    image_status_url = 'https://{}/cfp/v1/imageinstall/status'.format(MULTIPLAY_HOST)
    image_status_params = { 'accountserviceid': account_service_id }

    install_complete = False

    while not install_complete:
        image_status_response = requests.get(
            image_status_url,
            params = image_status_params,
            auth = auth,
            headers = MULTIPLAY_API_HEADERS
        )
        image_status_response.raise_for_status() # TODO: manually check for expected response

        image_status = json.loads(image_status_response.content.decode('utf-8'))
        install_complete = len(image_status.get('installs')) == 0

        print('Image Install Status: {}\n'.format(image_status))

        time.sleep(1.0)

if __name__ == "__main__":
    main()