# Simplified Protocol

## Caveats

* This folder proposes a simplified protocol for commanding an ARIS. Until this reaches the `master` branch, this should be considered "not released." The targeted milestone is a future ARIScope 2.8 release.
* On completion, this protocol will *likely* guarantee that the ARIS samples are correctly reordered, rather than requiring integrators' software to reorder samples. The earlier protocol will continue to require integrators' software reordering per the reordered flag in order to provide continuity for past integrations.

## Command Format

This protocol uses a TCP stream to control an ARIS, which is also done with the protocol buffer-based protocol described in the related SDK documentation.

However, in this case the commands are text-based, and protocol buffer isn't necessary. A command consists of a command name followed by 0 or more lines of key-value pairs. The command is terminated by an empty line.

For illustration purposes only, we're showing new lines as `\n` here:

```
  lightbulb\n
  enable=true\n
  rgb=255,0,0\n
  \n
```

The command name is alone on the first line. All command names and argument names are lower case.

Command parameters are passed as key-value pairs, where the key is the argument name. In the case of duplicates, the last value stated is used.

The value of a key-value pair is everything to the right of the `'='` character, until the end of the line. The newline character (`'\n'`) is required; carriage return (`'\r'`) is ignored.

## Commands

### `initialize`

`initialize` is required every time a connection is made, and must be the first command given. Nothing should be sent to the ARIS before this command. The first bytes of data received on the TCP stream dictate whether the simplified protocol is in use; therefore, the first line of the `initialize` command should be a single write to the TCP stream.

| Parameter | Description |
|-|-|
| `salinity` | Required. There are three valid values: `fresh`, `brackish`, and `saltwater`. Salinity affects the speed of sound in water, and affects calculations involving time and distance. |
| `feedback` | Optional, false by default. Valid values are `true` and `false`. This controls whether the ARIS responds to commands with descriptive feedback text, which may be useful during integration. This parameter may have little value after your integration is complete. If `false`, the ARIS sends nothing to the integrator's over the TCP stream. <br/>*Note that you can use WireShark or other network tools to watch the command & response from the ARIS while `feedback` is `true.`* |
| `datetime` | Setting the date and time should be considered mandatory by all integrators. If the RTC were to fail, times would not be consistent across power cycles. The required format is `2017-Apr-01 13:24:35`; US English is assumed for the month abbreviation. See below for more on formatting this parameter. |
| `rcvrport` | Required. This is the port number on which you will receive frames. You should open your receive socket before connecting to the ARIS. |
| `rcvrip` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will send frames to the host that opened the command connection. |

#### Formatting the `datetime` parameter

The ARIS expects the datetime parameter to be in the form

```
  2019-Apr-01 13:24:35
```

The ARIS also expects the month abbreviation to be from the en_US locale. If your client software uses `strftime()` but runs on a computer with a different locale, it could fail to set the ARIS' time properly.

We provide example code that formats the datetime value in a locale-invariant fashion, found in function `format_invariant_datetime()` [here](https://github.com/SoundMetrics/aris-integration-sdk/blob/94f2a5b1fd5c6c77089619aca9b6a890ee957531/common/code/CommandBuilder/CommandBuilder.cpp#L150).

### *(more commands to come)*

## Frame Format

In the simplified protocol, ARIS image data will be sent via UDP. One frame is broken up into multiple UDP datagrams, allowing us to adjust inter-datagram timing to accommodate slower network equipment. The payloads in multiple datagrams with the same frame index must be reassembled into a complete frame.

This table describes the header that starts the payload on each datagram. (All integral types are little-endian.)

| Field | Type | Offset | Description |
|-|-|-|-|
| `part_ header_ size` | `uint32_t` | 0 | The size of this header, up to but not including `payload`. `payload`  follows the header immediately. |
| `frame_ size` | `uint32_t` | 4 | This is the size of the ARIS frame header (1024 bytes) + the size of the frame's sample data. In other words, after you've reassembled the frame parts, this is 1024 + &lsaquo;total samples&rsaquo;. |
| `sequence_ number` | `uint32_t` | 8 | Represents the location of this part's `payload` in the reassembled frame. The first datagram's sequence number is 0. If the frame's first datagram carried only the frame header (which is 1024 bytes), the second datagram's sequence number would be 1024. |
| `frame_ index` | `int32_t` | 12 | Identifies this frame. There are generally multiple datagrams per frame. If `frame_index` changes before the complete frame is received, the previous frame is incomplete. |
| `payload` | `uint8_t[]` | `part_ header_ size` |  Payload bytes. The length of this field is the &lsaquo;datagram length&rsaquo; &thinsp;&ndash; `part_header_size`. |

Datagrams forming a very small frame could look those below, where there are 128 beams in the frame and 10 samples per beam.

The first datagram contains the frame header in `payload` followed by 460 of the 1280 sample bytes. (Total sample count is `beams` &times; `samples_per_beam`.)

**Datagram 0** &mdash; 1500 bytes

| Offset | Field | Value |
|-|-|-|
| 0 | `part_header_size` | 16 |
| 4 | `frame_size` | 2304 *(1024 + [128 &times; 10])* |
| 8 | `sequence_number` | 0 |
| 12 | `frame_index` | 0 |
| 16 | `payload` | &lsaquo;1024 bytes of frame header followed by 460 sample bytes&rsaquo; |

**Datagram 1** &mdash; 836 bytes

| Offset | Field | Value |
|-|-|-|
| 0 | `part_header_size` | 16 |
| 4 | `frame_size` | 2304 *(1024 + [128 &times; 10])* |
| 8 | `sequence_number` | 1484 *(the previous datagram contained 1024 bytes of frame header and 460 bytes of sample data)* |
| 12 | `frame_index` | 0 |
| 16 | `payload` | &lsaquo;820 sample bytes&rsaquo;

> Note: frame indexes in ARIS and ARIS (`.aris`) recordings are numbered from 0, but are presented to users as if numbered from 1.

The ARIS frame header is defined [here](https://github.com/SoundMetrics/aris-file-sdk/blob/546a0fe948fa358eeab70b3238f1802552c3a6f8/type-definitions/C/FrameHeader.h#L16).
