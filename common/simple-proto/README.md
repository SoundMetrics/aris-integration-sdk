# Simplified Protocol

## Caveats

This folder proposed a simplified protocol for commanding an ARIS. Until this reaches the `master` branch, this should be considered "not released." The targeted milestone is a future ARIScope 2.8 release.

## Command Format

This protocol uses a TCP stream to control an ARIS, which is also done with the protocol buffer-based protocol described in the related SDK documentation.

However, in this case the commands are text-based, and protocol buffer isn't necessary. A command consists of command name followed by 0 or more lines of key-value pairs. The command is terminated by a blank line.

For illustration, we're showing new lines as `\n` here:

```
  light\n
  enable=true\n
  rgb=255,0,0\n
  \n
```

The command name is alone on the first line. All command names and key-value keys are lower case.

Command parameters are passed as key-value pairs. In the case of duplicates, the last value stated is used.

The value of a key-value pair is everything to the right of the `'='` character, until the end of the line. The newline character (`'\n'`) is required; carriage return (`'\r'`) is ignored.

## Commands

### `initialize`

`initialize` is required every time a connection is made, and must be the first command given. Nothing should be sent to the ARIS before this command. The first bytes of data received on the TCP stream dictate whether the simplified protocol is in use; therefore, the first line of the `initialize` command should be a single write to the TCP stream.

| Parameter | Description |
|-|-|
| `salinity` | Required. There are three valid values: `fresh`, `brackish`, and `saltwater`. Salinity affects the speed of sound in water, and affects calculations involving time and distance. |
| `report` | Optional, false by default. Valid values are `true` and `false`. This controls whether the ARIS responds to commands with descriptive text, which may be useful during integration. This parameter may have little value after your integration is complete. If `false`, the ARIS sends nothing to the client over the TCP stream. |
| `datetime` | Setting the date and time should be considered mandatory by all clients. If the RTC were to fail, times would not be consistent across power cycles. The required format is `2017-Apr-01 13:24:35`; US English is assumed for the month abbreviation. See below for more on formatting this parameter. |
| `rcvrport` | Required. This is the port number on which you will receive frames. You should open your receive socket before connecting to the ARIS. |
| `rcvrip` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will send frames to the host that opened the command connection. |

#### Formatting the `datetime` parameter

The ARIS expects the datetime parameter to be in the form

```
  2019-Apr-01 13:24:35
```

The ARIS also expects the month abbreviation to be from the en_US locale. If your client uses `strftime()` but runs on a computer with a different locale, it could fail to set the ARIS' time properly.

We provide example code that formats the datetime value in a locale-invariant fashion is found in function `format_invariant_datetime()` in

```
  common\code\CommandBuilder\CommandBuilder.cpp
```

### (more commands to come)

## Frame Format

In the simplified protocol, ARIS image data will be sent via UDP. One frame is broken up into multiple UDP datagrams, allowing us to adjust inter-datagram timing to accommodate slower network equipment. The payloads in multiple datagrams with the same frame index must be reassembled into a complete frame.

This table describes the header that starts the payload on each datagram. (All integral types are little-endian.)

| Field | Type | Description |
|-|-|-|
| Part header size | uint32_t | The size of this header. Payload data follows the header immediately. |
| Frame size | uint32_t | This is the size of the ARIS frame header (1024 bytes) plus the size of the frame's sample data. In other words, after you've reassembled the frame parts, this is the size of the header + samples. |
| Sequence number | uint32_t | Represents the location of this part's payload in the reassembled frame. The first datagram's sequence number is zero. If the frame's first datagram carried only the frame header (1024 bytes), the second datagram's sequence number would be 1024. |
| Frame index | int32_t | Identifies this frame. If frame index changes before the complete frame is received, the previous frame is incomplete. |
| Payload | uint8_t[] |  Payload bytes. The size of this field is the datagram size less the part header size. |

