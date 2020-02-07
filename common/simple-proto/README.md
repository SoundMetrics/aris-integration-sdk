# Simplified Protocol

## Caveats

* This folder proposes a simplified protocol for commanding an ARIS. Until this reaches the `master` branch, this should be considered "not released." The targeted milestone is a future ARIScope 2.8 release.
* On completion, this protocol will *likely* guarantee that the ARIS samples are correctly reordered, rather than requiring integrators' software to reorder samples. The earlier protocol will continue to require integrators' software reordering per the reordered flag in order to provide continuity for past integrations.

## Command Format

This protocol uses a TCP stream to control an ARIS, which is also done with the protocol buffer-based protocol described in the related SDK documentation. In the case of this simplified protocol, client software should also read from the TCP stream as the ARIS will send feedback to commands.

In this protocol, the commands are text-based, and protocol buffer isn't necessary. A command consists of a command name followed by 0 or more lines of key-value pairs. The command is terminated by an empty line.

*Note that you can use WireShark or other network tools to observe the command & response from the ARIS.*

For illustration purposes only, we're showing new lines as `\n` here:

```
  lightbulb\n
  enable true\n
  rgb=255,255,255\n
  \n
```

The command name is alone on the first line. All command names and argument names are lower case.

Command parameters are passed as key-value pairs, separated by a space, where the key is the argument name. In the case of duplicates, the last value stated is used.

The value of a key-value pair is everything to the right of the parameter name, until the end of the line. The newline character (`'\n'`) is required; carriage return (`'\r'`) is ignored.

## Commands

### `initialize`

`initialize` is required every time a connection is made, and must be the first command given. Nothing should be sent to the ARIS before this command. The first bytes of data received on the TCP stream dictate whether the simplified protocol is in use; the first line of the `initialize` command must be the first data written to the TCP stream.

| Parameter | Description |
|-|-|
| `salinity` | **Required.** There are three valid values: `fresh`, `brackish`, and `saltwater`. Salinity affects the speed of sound in water, and affects calculations involving time and distance. |
| `datetime` | **Required.** Setting the date and time should be considered mandatory by all integrators. If the RTC were to fail, times would not be consistent across power cycles. The required format is `2017-Apr-01 13:24:35`; US English is assumed for the month abbreviation. See below for more on formatting this parameter. |
| `rcvr_port` | **Required.** This is the port number on which you will receive frames. You should open your receive socket before requesting frame data from the ARIS. |
| `rcvr_ip` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will send frames to the host that opened the command connection. |
| `rcvr_syslog` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will relay syslog messages to the host that opened the command connection. |

#### Example `initialize` Command

Newlines are shown for illustration.

```
initialize\n
salinity brackish\n
rcvr_port 52833\n
datetime 2020-Jan-14 10:57:42\n
\n
```

#### Example Feedback

```
Welcome to ARIS rev 2.8.8689 Simplified Protocol
Feedback for 'initialize':
Found required fields for initialize.
```

#### Formatting the `datetime` parameter

The ARIS expects the datetime parameter to be in the form

```
2019-Apr-01 13:24:35
```

The ARIS also expects the month abbreviation to be from the en_US locale. If your client software uses `strftime()` but runs on a computer with a different locale, it could fail to set the ARIS' time properly.

We provide example code that formats the datetime value in a locale-invariant fashion, found in function `format_invariant_datetime()` [here](https://github.com/SoundMetrics/aris-integration-sdk/blob/94f2a5b1fd5c6c77089619aca9b6a890ee957531/common/code/CommandBuilder/CommandBuilder.cpp#L150).

### `testpattern`

Sending the `testpattern` command causes the ARIS to return frames containing a test pattern. This may be used for testing during integration with client software.

This command has no parameters.

### `passive`

Sending the `passive` command causes the ARIS to acquire images without transmitting. This may be used for observing the effect of electrical noise on a vehicle.

### `acquire`

`acquire` sets the ARIS imaging parameters such that image data will be assembled into frames and delivered to the client software.

(Note that, unlike the protobuf-based protocol, data returned via the simplified protocol is already in correct order for display. Beam 0 remains on the right side of the image.)

| Parameter | Description |
|-|-|
| `start_range` | **Required.** The nearest edge of the image, in meters. |
| `end_range` | **Required.** The farthest edge of the image, in meters. |
| `frame_rate` | Optional. If not provided, the ARIS will use the fastest frame rate possible, up to 15 frames per second. Valid values are 1.0 to 15.0. ARIS will constrain this value as needed if required by the laws of physics. |
| `beams` | Optional. Allowed values are `full` and `half`. `full` denotes a higher cross-range resolution. If not provided, the ARIS will use `full` beams. |
| `samples_per_beam` | Optional, default is 1000. Valid range is 200 &ndash; 4000. |

#### Environmental Effects

Water temperature, depth, and salinity have an effect on computed speed of sound which, in turn, affects parameters used in image acquisition. Client software indicates salinity during protocol initialization, but water temperature and depth can vary over time.

`acquire` uses the water temperature and depth at the time the `acquire` command is received to determine parameters for image acquisition. ARIS does not update those parameters if the measured water temperature or depth changes&mdash;to avoid modifying image acquisition settings during operations.

Note that water temperature and depth readings can change drastically between "on deck" and "in the water."

Ranges...

More about beams & tradeoffs...

| Model | Full beams | Half beams |
|-|-|-|
| ARIS 1200 | 48  | n/a |
| ARIS 1800 | 96  | 48  |
| ARIS 3000 | 128 | 64  |

Gain

Frequency

### `stop-acquire`

`stop-acquire` stops frame acquisition. The client connection to the ARIS is still intact.

### *(more commands to come)*

## Frame Format

In the simplified protocol, ARIS image data will be sent via UDP. One frame is broken up into multiple UDP datagrams, allowing us to adjust inter-datagram timing to accommodate slower network equipment. The payloads in multiple datagrams with the same frame index must be reassembled into a complete frame.

This table describes the header that starts the payload on each datagram. (All integral types are little-endian.)

| Field | Type | Offset | Description |
|-|-|-|-|
| `signature` | `uint32_t` | 0 | Contains the value `0x53495241` (little-endian; "ARIS"). |
| `header_ size` | `uint32_t` | 4 | The size of this header, up to but not including `payload`. `payload`  follows the header immediately. |
| `frame_ size` | `uint32_t` | 8 | This is the size of the ARIS frame header (1024 bytes) + the size of the frame's sample data. In other words, after you've reassembled the frame parts, this is 1024 + &lsaquo;total samples&rsaquo;. |
| `frame_ index` | `uint32_t` | 12 | Identifies this frame. There are generally multiple datagrams per frame. If `frame_index` changes before the complete frame is received, the previous frame is incomplete. |
| `part_ number` | `uint32_t` | 16 | A zero based index of the parts of the current frame. The first datagram's sequence number is 0. Frame's first datagram carries only the frame header (which is 1024 bytes), the second datagram's payload contains the first samples of the frame. |
| `payload_size` | `uint32_t` | 20 | The number of octets in the payload. |
| `payload` | `uint8_t[]` | `parkt_ header_ size` |  Payload bytes. The length of this field is the &lsaquo;datagram length&rsaquo; &thinsp;&ndash; `part_header_size`. |

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
