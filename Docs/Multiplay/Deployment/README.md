# Deploying to Multiplay

## Getting Started
__Note:__ Though this example is written with instructions to deploy from a Linux distro, it is not necessary to do so. So long as the dedicated server you are deploying is compatible with the fleet type Multiplay has provided you, where the deployment is run from does not matter.

### WSL
This project makes use of Multiplay's Ubuntu 18.04 boxes. We will be walking you through assuming a fresh install of 18.04 locally. If you are running a Windows machine, you will want to install WSL. Instructions for setting up WSL can be found [here](https://docs.microsoft.com/en-us/windows/wsl/install-win10#manual-installation-steps)]. We are using WSL 1.

### Python
The 18.04 distro comes with Python 3 installed. You will need to install `pip` for package management though.

```
$ sudo apt update
$ sudo apt install python3-pip
$ pip3 --version
```

### AWS CLI
The machine that runs the deploy script should have the AWS CLI installed. Instructions for installation can be found [here](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2-linux.html). We are using V2 of the CLI. Once the it is installed, it must be configured with credentials for an AWS account. Instructions for configuration can be found [here](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html). These credentials should have read/write permissions to an S3 bucket. Here is an example IAM policy for the credentials that gives full access to our bucket.

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": "s3:*",
            "Resource": "arn:aws:s3:::<your-bucket-name>*"
        }
    ]
}
```

__Note:__ You will want to replace the bucket name in this policy to match your own bucket.

If you opt to store AWS/Multiplay credentials in AWS SecretsManager, the following is an example IAM policy that give read access to that secret.

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "secretsmanager:GetSecretValue",
                "secretsmanager:DescribeSecret",
                "secretsmanager:ListSecretVersionIds"
            ],
            "Resource": "arn:aws:secretsmanager:<region>:<aws-account-id>:secret:<your-secret-name>*"
        }
    ]
}
```

__Note:__ You will want to replace the secret name in this policy to match your own secret.

__Note:__ If you are new to AWS, you may want to use an access key/secret key in your AWS CLI that have superuser access to verify that things are working and then reduce permissions to only access scoped resources using the IAM policies above.

The format of the secret that the deploy script is expecting is:

```
{
  "aws_access_key": "",
  "aws_secret_access_key": "",
  "multiplay_access_key": "",
  "multiplay_secret_access_key": "",
  "account_service_id": "",
  "build_machine_id": "",
  "image_a_id": "",
  "image_b_id": ""
}
```

The AWS Access Key and Secret Access Key should have IAM policies attached that scope access to the S3 bucket where you are uploading your builds. The other Multiplay values are provided to you by Multiplay in a service identifier PDF during your onboarding process.

## Deploy Script
The deploy script aims to be an example that helps to familiarize you with the update flow. It does not exercise all API endpoints that are available for game images, but it does provide a blueprint for a basic deployment. You will notice that most of the steps follow a similar pattern of:
1. Create a job
2. Poll the job
3. Use output of job to create next job

To run the deploy script, first install the required packages.

```
$ pip3 install -r requirements.txt
```

Next, run the script.

```
$ python3 multiplay_deploy.py --version <game-version> --server-path <local-path-to-server> --s3-bucket-name <s3-bucket-for-storage> --image <a-or-b> --secret-name <secretsmanager-secret-name>
```

## Testing Your Deployment
Once your deployment is complete, you should be able to test it with a client that is compatible. Multiplay accepts both AWS Signature 4 authentication or HTTP Basic Auth. For the following requests we will be using HTTP Basic Auth.

```
# First, set your variables
$ export MULTIPLAY_ACCESS_KEY_ID=<your-multiplay-access-key-id>
$ export MULTIPLAY_SECRET_ACCESS_KEY=<your-multiplay-secret-access-key>
$ export BASIC_AUTH_STRING=$(echo -n $MULTIPLAY_ACCESS_KEY_ID:$MULTIPLAY_SECRET_ACCESS_KEY | base64 | tr -d '\n\r')
$ export ACCOUNT_SERVICE_ID=<your-multiplay-account-service-id>
$ export BUILD_MACHINE_ID=<your-multiplay-build-machine-id>
$ export IMAGE_A_ID=<your-multiplay-image-a-id>
$ export IMAGE_B_ID=<your-multiplay-image-b-id>
$ export FLEET_ID=<your-fleet-id>
$ export PROFILE_ID=<your-profile-id>
$ export REGION_ID=<your-region-id>

# Alternatively, paste your variables into the `set-env-vars.sh` and run
$ source ./set-env-vars.sh
```

The first step is to allocate your server.

https://docs.multiplay.com/game_servers/server_allocate/
```
$ curl --request GET \
    --url "https://api.multiplay.co.uk/cfp/v1/server/allocate?fleetid=$FLEET_ID&profileid=$PROFILE_ID&regionid=$REGION_ID&uuid=$(uuidgen)" \
    --header "Authorization: Basic $BASIC_AUTH_STRING"

# Set the UUID from the request output
$ export UUID=<uuid-from-allocation-request>
```

You may now begin polling the allocations endpoint. If you do not receive an `ip` and `port` after a few minutes, some something has likely gone wrong. Once you do receive an `ip` and `port` you may connect to your game server from your client.

https://docs.multiplay.com/game_servers/server_allocations/
```
$ curl --request GET \
    --url "https://api.multiplay.co.uk/cfp/v1/server/allocations?fleetid=$FLEET_ID&uuid=$UUID" \
    --header "Authorization: Basic $BASIC_AUTH_STRING"
```

When you are done with the server, it is time to clean it up.

https://docs.multiplay.com/game_servers/server_deallocate/
```
$ curl --request GET \
    --url "https://api.multiplay.co.uk/cfp/v1/server/deallocate?fleetid=$FLEET_ID&uuid=$UUID" \
    --header "Authorization: Basic $BASIC_AUTH_STRING"
```