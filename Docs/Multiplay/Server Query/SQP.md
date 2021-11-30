# Server Queries
You can query information from a running game server using UDP/IP packets. This document describes the packet formats and protocol to access this data.


## Data Types
All server queries consist of four basic types of data packed together into a data stream. All types are big endian.

| Name | Description |
| --- | --- |
| byte | 8 bit character or unsigned integer |
| ushort | 16 bit unsigned integer |
| uint | 32 bit unsigned integer |
| ulong | 64 bit unsigned integer |
| string | variable-length byte field, encoded in UTF-8, prefixed with a byte containing the length of the string |

## Requests
The server response to 5 queries:

`ChallengeRequest`
> Returns a challenge number for use in query request responses

`QueryRequest` with `ServerInfo` chunk
> Basic information about the server

`QueryRequest` with `ServerRules` chunk
> The rules the server is using. Not required by Multiplay

`QueryRequest` with `PlayerInfo` chunk
> Details about each player on the server. Not required by Multiplay

`QueryRequest` with `TeamInfo` chunk
> Details about the teams on the server. Not required by Multiplay


## Packet Format

### Types
There are four different types of SQP packet.
| Packet Type | Comment |
| --- | --- |
| ChallengeRequest | Byte value 0 |
| ChallengeResponse | Byte value 0 |
| QueryRequest | Byte value 1 |
| QueryResponse | Byte value 1 |


### All Packets - Header
All SQP packets, whether they are a request or response contain a header.
| Data | Type | Comment |
| --- | --- | --- |
| Type | byte | See Packet Types |
| ChallengeToken | uint | Challenge number, required for QueryRequest and QueryResponse packet types |
| Payload| | Body of the packet, depending on the packet type


## Challenge Packets
Retrieves a usable challenge number for a subsequent request.

_Note:_ There is not payload for challenge packets. Only a header.

### Request Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |

Example ChallengeRequest packet:

    [0x00000000]	0x00 '\0'	unsigned char
    [0x00000001]	0x00 '\0'	unsigned char
    [0x00000002]	0x00 '\0'	unsigned char
    [0x00000003]	0x00 '\0'	unsigned char
    [0x00000004]	0x00 '\0'	unsigned char


### Response Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |

Example ChallengeResponse packet:

    [0x00000000]	0x00 '\0'	unsigned char
    [0x00000001]	0x80 '€'	unsigned char
    [0x00000002]	0x90 ''	unsigned char
    [0x00000003]	0x23 '#'	unsigned char
    [0x00000004]	0x48 'H'	unsigned char



## Query Packets
Retrieves information about the server. Multiplay only requires responses to queries with `ServerInfo` chunks, the other request types may be ignored if you wish.

### Chunk Types
There are four different types of chunks that may be requested.
| Chunk Type | Comment |
| --- | --- |
| ServerInfo | Byte value 1 << 0 |
| ServerRules | Byte value 1 << 1 |
| PlayerInfo | Byte value 1 << 2 |
| TeamInfo | Byte value 1 << 3 |

### ServerInfo Request Format
**Note:** This is the only request that requires a response.

| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP |
| RequestedChunks | byte | ChunkType(s) that are being requested |

Example QueryRequest packet with ServerInfo RequestedChunk:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0x80 '€'	unsigned char
    [0x00000002]	0x31 '1'	unsigned char
    [0x00000003]	0xbe '¾'	unsigned char
    [0x00000004]	0x18 '\x18'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x01 '\x1'	unsigned char
   

### ServerInfo Response Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP, received from request |
| CurrentPacket | byte | 0 |
| LastPacket | byte | 0 |
| PacketLength | ushort | Length of the packet, after this point |
| ChunkLength | uint | Length of ServerInfo chunk, after this point |
| CurrentPlayers | ushort | Number of players on the server |
| MaxPlayers | ushort | Maximum number of players the server supports |
| ServerName | string | Name of the server |
| GameType | string | Type of game on the server |
| BuildId | string | Version/build ID of the server |
| Map | string | Map the server currently has loaded |
| Port | ushort | The game port the server has exposed |

Example QueryResponse packet:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0xc0 'À'	unsigned char
    [0x00000002]	0x7a 'z'	unsigned char
    [0x00000003]	0x6c 'l'	unsigned char
    [0x00000004]	0x3d '='	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x00 '\0'	unsigned char
    [0x00000008]	0x00 '\0'	unsigned char
    [0x00000009]	0x00 '\0'	unsigned char
    [0x0000000a]	0x5b '['	unsigned char
    [0x0000000b]	0x00 '\0'	unsigned char
    [0x0000000c]	0x00 '\0'	unsigned char
    [0x0000000d]	0x00 '\0'	unsigned char
    [0x0000000e]	0x57 'W'	unsigned char
    [0x0000000f]	0x00 '\0'	unsigned char
    [0x00000010]	0x00 '\0'	unsigned char
    [0x00000011]	0x00 '\0'	unsigned char
    [0x00000012]	0x10 '\x10'	unsigned char
    [0x00000013]	0x14 '\x14'	unsigned char
    [0x00000014]	0x55 'U'	unsigned char
    [0x00000015]	0x45 'E'	unsigned char
    [0x00000016]	0x34 '4'	unsigned char
    [0x00000017]	0x20 ' '	unsigned char
    [0x00000018]	0x44 'D'	unsigned char
    [0x00000019]	0x65 'e'	unsigned char
    [0x0000001a]	0x64 'd'	unsigned char
    [0x0000001b]	0x69 'i'	unsigned char
    [0x0000001c]	0x63 'c'	unsigned char
    [0x0000001d]	0x61 'a'	unsigned char
    [0x0000001e]	0x74 't'	unsigned char
    [0x0000001f]	0x65 'e'	unsigned char
    [0x00000020]	0x64 'd'	unsigned char
    [0x00000021]	0x20 ' '	unsigned char
    [0x00000022]	0x53 'S'	unsigned char
    [0x00000023]	0x65 'e'	unsigned char
    [0x00000024]	0x72 'r'	unsigned char
    [0x00000025]	0x76 'v'	unsigned char
    [0x00000026]	0x65 'e'	unsigned char
    [0x00000027]	0x72 'r'	unsigned char
    [0x00000028]	0x2e '.'	unsigned char
    [0x00000029]	0x2f '/'	unsigned char
    [0x0000002a]	0x53 'S'	unsigned char
    [0x0000002b]	0x63 'c'	unsigned char
    [0x0000002c]	0x72 'r'	unsigned char
    [0x0000002d]	0x69 'i'	unsigned char
    [0x0000002e]	0x70 'p'	unsigned char
    [0x0000002f]	0x74 't'	unsigned char
    [0x00000030]	0x2f '/'	unsigned char
    [0x00000031]	0x53 'S'	unsigned char
    [0x00000032]	0x68 'h'	unsigned char
    [0x00000033]	0x6f 'o'	unsigned char
    [0x00000034]	0x6f 'o'	unsigned char
    [0x00000035]	0x74 't'	unsigned char
    [0x00000036]	0x65 'e'	unsigned char
    [0x00000037]	0x72 'r'	unsigned char
    [0x00000038]	0x47 'G'	unsigned char
    [0x00000039]	0x61 'a'	unsigned char
    [0x0000003a]	0x6d 'm'	unsigned char
    [0x0000003b]	0x65 'e'	unsigned char
    [0x0000003c]	0x2e '.'	unsigned char
    [0x0000003d]	0x53 'S'	unsigned char
    [0x0000003e]	0x68 'h'	unsigned char
    [0x0000003f]	0x6f 'o'	unsigned char
    [0x00000040]	0x6f 'o'	unsigned char
    [0x00000041]	0x74 't'	unsigned char
    [0x00000042]	0x65 'e'	unsigned char
    [0x00000043]	0x72 'r'	unsigned char
    [0x00000044]	0x47 'G'	unsigned char
    [0x00000045]	0x61 'a'	unsigned char
    [0x00000046]	0x6d 'm'	unsigned char
    [0x00000047]	0x65 'e'	unsigned char
    [0x00000048]	0x5f '_'	unsigned char
    [0x00000049]	0x54 'T'	unsigned char
    [0x0000004a]	0x65 'e'	unsigned char
    [0x0000004b]	0x61 'a'	unsigned char
    [0x0000004c]	0x6d 'm'	unsigned char
    [0x0000004d]	0x44 'D'	unsigned char
    [0x0000004e]	0x65 'e'	unsigned char
    [0x0000004f]	0x61 'a'	unsigned char
    [0x00000050]	0x74 't'	unsigned char
    [0x00000051]	0x68 'h'	unsigned char
    [0x00000052]	0x4d 'M'	unsigned char
    [0x00000053]	0x61 'a'	unsigned char
    [0x00000054]	0x74 't'	unsigned char
    [0x00000055]	0x63 'c'	unsigned char
    [0x00000056]	0x68 'h'	unsigned char
    [0x00000057]	0x03 '\x3'	unsigned char
    [0x00000058]	0x30 '0'	unsigned char
    [0x00000059]	0x30 '0'	unsigned char
    [0x0000005a]	0x31 '1'	unsigned char
    [0x0000005b]	0x08 '\b'	unsigned char
    [0x0000005c]	0x48 'H'	unsigned char
    [0x0000005d]	0x69 'i'	unsigned char
    [0x0000005e]	0x67 'g'	unsigned char
    [0x0000005f]	0x68 'h'	unsigned char
    [0x00000060]	0x72 'r'	unsigned char
    [0x00000061]	0x69 'i'	unsigned char
    [0x00000062]	0x73 's'	unsigned char
    [0x00000063]	0x65 'e'	unsigned char
    [0x00000064]	0x1e '\x1e'	unsigned char
    [0x00000065]	0x61 'a'	unsigned char


### ServerRules Request Format
**Note:** Implementing a response to this request is optional.

| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP |
| RequestedChunks | byte | ChunkType(s) that are being requested |

Example QueryRequest packet with ServerRules RequstedChunk:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0xa0 ' '	unsigned char
    [0x00000002]	0x1d '\x1d'	unsigned char
    [0x00000003]	0xcc 'Ì'	unsigned char
    [0x00000004]	0x0e '\xe'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x02 '\x2'	unsigned char


### ServerRules Response Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP, received from request |
| CurrentPacket | byte | 0 |
| LastPacket | byte | 0 |
| PacketLength | ushort | Length of the packet, after this point |
| ChunkLength | uint | Length of ServerRules chunk, after this point |

Directly after the `ChunkLength`, for every rule that there is:
| Data | Type | Comment |
| --- | --- | --- |
| Key | string | The key, or name, of the rule |
| Type | byte | 0 for byte, 1 for ushort, 2 for uint, 3 for ulong, and 4 for string |
| Value | variable, depending on preceding 'Type' byte | A value for the key, data type depending on preceding byte |

Example QueryResponse packet for ServerRules:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0x80 '€'	unsigned char
    [0x00000002]	0x31 '1'	unsigned char
    [0x00000003]	0xbe '¾'	unsigned char
    [0x00000004]	0x18 '\x18'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x00 '\0'	unsigned char
    [0x00000008]	0x00 '\0'	unsigned char
    [0x00000009]	0x00 '\0'	unsigned char
    [0x0000000a]	0x3c '<'	unsigned char
    [0x0000000b]	0x00 '\0'	unsigned char
    [0x0000000c]	0x00 '\0'	unsigned char
    [0x0000000d]	0x00 '\0'	unsigned char
    [0x0000000e]	0x38 '8'	unsigned char
    [0x0000000f]	0x0a '\n'	unsigned char
    [0x00000010]	0x52 'R'	unsigned char
    [0x00000011]	0x75 'u'	unsigned char
    [0x00000012]	0x6c 'l'	unsigned char
    [0x00000013]	0x65 'e'	unsigned char
    [0x00000014]	0x4f 'O'	unsigned char
    [0x00000015]	0x6e 'n'	unsigned char
    [0x00000016]	0x65 'e'	unsigned char
    [0x00000017]	0x4b 'K'	unsigned char
    [0x00000018]	0x65 'e'	unsigned char
    [0x00000019]	0x79 'y'	unsigned char
    [0x0000001a]	0x00 '\0'	unsigned char
    [0x0000001b]	0x10 '\x10'	unsigned char
    [0x0000001c]	0x0a '\n'	unsigned char
    [0x0000001d]	0x52 'R'	unsigned char
    [0x0000001e]	0x75 'u'	unsigned char
    [0x0000001f]	0x6c 'l'	unsigned char
    [0x00000020]	0x65 'e'	unsigned char
    [0x00000021]	0x54 'T'	unsigned char
    [0x00000022]	0x77 'w'	unsigned char
    [0x00000023]	0x6f 'o'	unsigned char
    [0x00000024]	0x4b 'K'	unsigned char
    [0x00000025]	0x65 'e'	unsigned char
    [0x00000026]	0x79 'y'	unsigned char
    [0x00000027]	0x01 '\x1'	unsigned char
    [0x00000028]	0x00 '\0'	unsigned char
    [0x00000029]	0x20 ' '	unsigned char
    [0x0000002a]	0x0c '\f'	unsigned char
    [0x0000002b]	0x52 'R'	unsigned char
    [0x0000002c]	0x75 'u'	unsigned char
    [0x0000002d]	0x6c 'l'	unsigned char
    [0x0000002e]	0x65 'e'	unsigned char
    [0x0000002f]	0x54 'T'	unsigned char
    [0x00000030]	0x68 'h'	unsigned char
    [0x00000031]	0x72 'r'	unsigned char
    [0x00000032]	0x65 'e'	unsigned char
    [0x00000033]	0x65 'e'	unsigned char
    [0x00000034]	0x4b 'K'	unsigned char
    [0x00000035]	0x65 'e'	unsigned char
    [0x00000036]	0x79 'y'	unsigned char
    [0x00000037]	0x04 '\x4'	unsigned char
    [0x00000038]	0x0e '\xe'	unsigned char
    [0x00000039]	0x52 'R'	unsigned char
    [0x0000003a]	0x75 'u'	unsigned char
    [0x0000003b]	0x6c 'l'	unsigned char
    [0x0000003c]	0x65 'e'	unsigned char
    [0x0000003d]	0x54 'T'	unsigned char
    [0x0000003e]	0x68 'h'	unsigned char
    [0x0000003f]	0x72 'r'	unsigned char
    [0x00000040]	0x65 'e'	unsigned char
    [0x00000041]	0x65 'e'	unsigned char
    [0x00000042]	0x56 'V'	unsigned char
    [0x00000043]	0x61 'a'	unsigned char
    [0x00000044]	0x6c 'l'	unsigned char
    [0x00000045]	0x75 'u'	unsigned char
    [0x00000046]	0x65 'e'	unsigned char


### PlayerInfo Request Format
**Note:** Implementing a response to this request is optional.

| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP |
| RequestedChunks | byte | ChunkType(s) that are being requested |

Example QueryRequest packet with PlayerInfo RequestedChunk:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0x80 '€'	unsigned char
    [0x00000002]	0x2a '*'	unsigned char
    [0x00000003]	0x3c '<'	unsigned char
    [0x00000004]	0x15 '\x15'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x04 '\x4'	unsigned char

### PlayerInfo Response Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP, received from request |
| CurrentPacket | byte | 0 |
| LastPacket | byte | 0 |
| PacketLength | ushort | Length of the packet, after this point |
| ChunkLength | uint | Length of PlayerInfo chunk, after this point |
| PlayerCount | ushort | Number of players |
| FieldCount | byte | Number of fields per player |

Directly after the `FieldCount`, for ever field:
| Data | Type | Comment |
| --- | --- | --- |
| Key | string | The key, or name, of the rule |
| Type | byte | 0 for byte, 1 for uint16, 2 for uint32, 3 for uint64, and 4 for string |

Directly after the final field, for every field on every player:
| Data | Type | Comment |
| --- | --- | --- |
| Value | variable, depending on the corresponding field's 'Type' byte | A value for the key, data type depending on corresponding field type |


Example QueryResponse packet for PlayerInfo:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0x52 'R'	unsigned char
    [0x00000002]	0x00 '\0'	unsigned char
    [0x00000003]	0x29 ')'	unsigned char
    [0x00000004]	0x00 '\0'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x00 '\0'	unsigned char
    [0x00000008]	0x00 '\0'	unsigned char
    [0x00000009]	0x00 '\0'	unsigned char
    [0x0000000a]	0x3e '>'	unsigned char
    [0x0000000b]	0x00 '\0'	unsigned char
    [0x0000000c]	0x00 '\0'	unsigned char
    [0x0000000d]	0x00 '\0'	unsigned char
    [0x0000000e]	0x3a ':'	unsigned char
    [0x0000000f]	0x00 '\0'	unsigned char
    [0x00000010]	0x01 '\x1'	unsigned char
    [0x00000011]	0x03 '\x3'	unsigned char
    [0x00000012]	0x04 '\x4'	unsigned char
    [0x00000013]	0x6e 'n'	unsigned char
    [0x00000014]	0x61 'a'	unsigned char
    [0x00000015]	0x6d 'm'	unsigned char
    [0x00000016]	0x65 'e'	unsigned char
    [0x00000017]	0x04 '\x4'	unsigned char
    [0x00000018]	0x05 '\x5'	unsigned char
    [0x00000019]	0x73 's'	unsigned char
    [0x0000001a]	0x63 'c'	unsigned char
    [0x0000001b]	0x6f 'o'	unsigned char
    [0x0000001c]	0x72 'r'	unsigned char
    [0x0000001d]	0x65 'e'	unsigned char
    [0x0000001e]	0x02 '\x2'	unsigned char
    [0x0000001f]	0x08 '\b'	unsigned char
    [0x00000020]	0x64 'd'	unsigned char
    [0x00000021]	0x75 'u'	unsigned char
    [0x00000022]	0x72 'r'	unsigned char
    [0x00000023]	0x61 'a'	unsigned char
    [0x00000024]	0x74 't'	unsigned char
    [0x00000025]	0x69 'i'	unsigned char
    [0x00000026]	0x6f 'o'	unsigned char
    [0x00000027]	0x6e 'n'	unsigned char
    [0x00000028]	0x03 '\x3'	unsigned char
    [0x00000029]	0x13 '\x13'	unsigned char
    [0x0000002a]	0x48 'H'	unsigned char
    [0x0000002b]	0x75 'u'	unsigned char
    [0x0000002c]	0x6e 'n'	unsigned char
    [0x0000002d]	0x74 't'	unsigned char
    [0x0000002e]	0x65 'e'	unsigned char
    [0x0000002f]	0x72 'r'	unsigned char
    [0x00000030]	0x73 's'	unsigned char
    [0x00000031]	0x2d '-'	unsigned char
    [0x00000032]	0x43 'C'	unsigned char
    [0x00000033]	0x6f 'o'	unsigned char
    [0x00000034]	0x6d 'm'	unsigned char
    [0x00000035]	0x70 'p'	unsigned char
    [0x00000036]	0x75 'u'	unsigned char
    [0x00000037]	0x74 't'	unsigned char
    [0x00000038]	0x65 'e'	unsigned char
    [0x00000039]	0x72 'r'	unsigned char
    [0x0000003a]	0x2e '.'	unsigned char
    [0x0000003b]	0x2e '.'	unsigned char
    [0x0000003c]	0x2e '.'	unsigned char
    [0x0000003d]	0x00 '\0'	unsigned char
    [0x0000003e]	0x00 '\0'	unsigned char
    [0x0000003f]	0x00 '\0'	unsigned char
    [0x00000040]	0x00 '\0'	unsigned char
    [0x00000041]	0x00 '\0'	unsigned char
    [0x00000042]	0x00 '\0'	unsigned char
    [0x00000043]	0x00 '\0'	unsigned char
    [0x00000044]	0x00 '\0'	unsigned char
    [0x00000045]	0x00 '\0'	unsigned char
    [0x00000046]	0x00 '\0'	unsigned char
    [0x00000047]	0x00 '\0'	unsigned char
    [0x00000048]	0x51 'Q'	unsigned char


### TeamInfo Request Format
**Note:** Implementing a response to this request is optional.

| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP |
| RequestedChunks | byte | ChunkType(s) that are being requested |

Example QueryRequest packet with a TeamInfo RequestedChunk:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0xd6 'Ö'	unsigned char
    [0x00000002]	0x03 '\x3'	unsigned char
    [0x00000003]	0xeb 'ë'	unsigned char
    [0x00000004]	0x01 '\x1'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x08 '\b'	unsigned char


### TeamInfo Response Format
| Data | Type | Comment |
| --- | --- | --- |
| Header | | Header of the packet. See "Packet Format -> All Packets - Header" |
| Version | ushort | Version of SQP, received from request |
| CurrentPacket | byte | 0 |
| LastPacket | byte | 0 |
| PacketLength | ushort | Length of the packet, after this point |
| ChunkLength | uint | Length of TeamInfo chunk, after this point |
| TeamCount | ushort | Number of players |
| FieldCount | byte | Number of fields per team |

Directly after the `FieldCount`, for ever field:
| Data | Type | Comment |
| --- | --- | --- |
| Key | string | The key, or name, of the rule |
| Type | byte | 0 for byte, 1 for uint16, 2 for uint32, 3 for uint64, and 4 for string |

Directly after the final field, for every field on every team:
| Data | Type | Comment |
| --- | --- | --- |
| Value | variable, depending on the corresponding field's 'Type' byte | A value for the key, data type depending on corresponding field type |


Example QueryResponse packet:

    [0x00000000]	0x01 '\x1'	unsigned char
    [0x00000001]	0x80 '€'	unsigned char
    [0x00000002]	0xe5 'å'	unsigned char
    [0x00000003]	0xae '®'	unsigned char
    [0x00000004]	0x72 'r'	unsigned char
    [0x00000005]	0x00 '\0'	unsigned char
    [0x00000006]	0x01 '\x1'	unsigned char
    [0x00000007]	0x00 '\0'	unsigned char
    [0x00000008]	0x00 '\0'	unsigned char
    [0x00000009]	0x00 '\0'	unsigned char
    [0x0000000a]	0x16 '\x16'	unsigned char
    [0x0000000b]	0x00 '\0'	unsigned char
    [0x0000000c]	0x00 '\0'	unsigned char
    [0x0000000d]	0x00 '\0'	unsigned char
    [0x0000000e]	0x12 '\x12'	unsigned char
    [0x0000000f]	0x00 '\0'	unsigned char
    [0x00000010]	0x02 '\x2'	unsigned char
    [0x00000011]	0x01 '\x1'	unsigned char
    [0x00000012]	0x05 '\x5'	unsigned char
    [0x00000013]	0x73 's'	unsigned char
    [0x00000014]	0x63 'c'	unsigned char
    [0x00000015]	0x6f 'o'	unsigned char
    [0x00000016]	0x72 'r'	unsigned char
    [0x00000017]	0x65 'e'	unsigned char
    [0x00000018]	0x02 '\x2'	unsigned char
    [0x00000019]	0x00 '\0'	unsigned char
    [0x0000001a]	0x00 '\0'	unsigned char
    [0x0000001b]	0x00 '\0'	unsigned char
    [0x0000001c]	0x00 '\0'	unsigned char
    [0x0000001d]	0x00 '\0'	unsigned char
    [0x0000001e]	0x00 '\0'	unsigned char
    [0x0000001f]	0x00 '\0'	unsigned char
    [0x00000020]	0x00 '\0'	unsigned char
