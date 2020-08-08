# Simplified Protocol

This document describes a simplified protocol for commanding an ARIS. The Protocol Buffer-based method of interacting with the sonar has occasionally been problematic for integration partners. This _Simplified Protocol_ consists of two parts:

* The **Command Protocol** is implemented over a 2-way TCP stream. ASCII-based commands are sent to the ARIS. An ASCII-based response is received from the ARIS, indicating success or failure.
* The **Frame Protocol** is implemented as a 1-way UDP stream of datagrams. The receiver must assemble the datagrams into a complete frame.

## Caveats ### REMOVE ON RELEASE

* This document _proposes_ a simplified protocol for commanding an ARIS. Until this reaches the `master` branch, this should be considered "not released." The targeted milestone is a future ARIScope 2.8 release.
* On completion, this protocol *may* guarantee that the ARIS samples are correctly reordered, rather than requiring integrators' software to reorder samples. The earlier protocol will continue to require integrators' software reordering per the reordered flag in order to provide continuity for past integrations.
* The name _Simplified Protocol_ may change.

## Command Protocol

This protocol uses a TCP stream to control an ARIS, which is also done with the protocol buffer-based protocol described in the related SDK documentation. In the case of this simplified protocol, client software should also read from the TCP stream as the ARIS will send feedback to commands.

In this protocol, the commands are text-based, and protocol buffer isn't necessary. A command consists of a command name followed by 0 or more lines of key-value pairs. The command is terminated by an empty line.

**Note:**
> You can use WireShark to observe the command & response from the ARIS as they pass over the network.

For illustration purposes only, we're showing new lines as `\n` here:

```txt
  lightbulb\n
  enable true\n
  rgb=255,255,255\n
  \n
```

The command name is alone on the first line. All command names and argument names are lower case.

Command parameters are passed as key-value pairs, separated by a space, where the key is the argument name. In the case of duplicates, the last value stated is used.

The value of a key-value pair is everything to the right of the parameter name, until the end of the line. The newline character (`'\n'`) is required; carriage return (`'\r'`) is ignored.

### Commands

#### `initialize`

`initialize` is required every time a connection is made, and must be the first command given. Nothing should be sent to the ARIS before this command. The first bytes of data received on the TCP stream dictate whether the simplified protocol is in use; the first line of the `initialize` command must be the first data written to the TCP stream.

| Parameter | Description |
|-|-|
| `salinity` | **Required.** There are three valid values: `fresh`, `brackish`, and `saltwater`. Salinity affects the speed of sound in water, and affects calculations involving time and distance. |
| `datetime` | **Required.** Setting the date and time should be considered mandatory by all integrators. If the ARIS' onboard clock were to fail, times would not be consistent across power cycles. The required format is `2017-Apr-01 13:24:35`; US English is assumed for the month abbreviation. See below for more on formatting this parameter. |
| `rcvr_port` | **Required.** This is the port number on which you will receive frames. You should open your receive socket before requesting frame data from the ARIS. |
| `rcvr_ip` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will send frames to the host that opened the command connection. |
| `rcvr_syslog` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will relay syslog messages to the host that opened the command connection. |

##### Example `initialize` Command

Newlines are shown for illustration.

```txt
  initialize\n
  salinity brackish\n
  datetime 2020-Mar-17 08:52:40\n
  rcvr_port 50681\n
  \n
```

#### Command Response for `initialize`

The ARIS will respond to the `initialize` command with the following:

```txt
  200 OK\n
  Welcome to ARIS\n
    ArisApp rev 2.8.8711\n
    Simplified Protocol enabled.\n
  Feedback for 'initialize':\n
  Setting salinity=15\n
  Sonar system date and time set to 2020-Mar-17 08:52:40\n
  \n
```

### Formatting the `datetime` parameter

The ARIS expects the datetime parameter to be in the form

```txt
2019-Apr-01 13:24:35
```

The ARIS also expects the month abbreviation to be from the en_US locale. If your client software uses `strftime()` but runs on a computer with a different locale, it could fail to set the ARIS' time properly.

We provide example code that formats the datetime value in a locale-invariant fashion, found in function `format_invariant_datetime()` [here](https://github.com/SoundMetrics/aris-integration-sdk/blob/94f2a5b1fd5c6c77089619aca9b6a890ee957531/common/code/CommandBuilder/CommandBuilder.cpp#L150).

### Command Response Codes

Command response codes are on the first line of the response from the ARIS. Possible codes are:

* `200 OK` &ndash; This indicates success. Note that when setting a range, the parameters given may be constrained to make the request comply with the the laws of physics, and the abilities of the ARIS.
* `400 Bad Request` &ndash; This indicates an error; the request contains an unexpected or out-of-range value.
* `404 Not Found` &ndash; This indicates an error; the ARIS does not understand the command requested.

**Please note:**
> The use of HTTP response codes in this protocol does not imply the availability of other capabilities found in the HTTP protocol. This is not an HTTP-based protocol.

### `testpattern`

Sending the `testpattern` command causes the ARIS to return frames containing a test pattern. This may be used for testing during integration with client software.

This command has no parameters.

#### Command Response for `testpattern`

The ARIS will respond to the `testpattern` command with the following:

```txt
  200 OK\n
  settings-cookie 1\n
  Feedback for 'testpattern':\n
  Applying settings.\n
  \n
```

### Settings Cookie

In the command response for `testpattern`, there is a line for the "settings cookie:"

```txt
  settings-cookie N\n
```

Here 'N' indicates the value of the settings cookie. Each successive acquisition attempt&mdash;including the `testpattern` and `passive` commands&mdash;is recorded with a new value for the settings cookie. The settings cookie also appears in the ARIS frame header, in either the `AppliedSettings` or `ConstrainedSettings` field.

**Note:**
> If settings are applied as-is, the cookie appears in the `AppliedSettings` field. If the settings were constrained prior to application, the cookie appears in the `ConstrainedSettings` field.

**Also note:**
> The `settings-cookie` value is made available primarily for testing purposes. However, if you use it for any purpose be aware that, due to latency in data acquisition, the frame received immediately after changing acquisition settings may have the previous settings' cookie value and settings applied.

### `passive`

Sending the `passive` command causes the ARIS to acquire images without transmitting. This may be used for observing the presence of electrical or acoustic noise on a vehicle.

#### Command Response for `passive`

```txt
  200 OK\n
  settings-cookie 2\n
  Feedback for 'passive':\n
  Applying settings.\n
  \n
```

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
| `frequency` | Optional, default is `auto`. Valid values are `auto`, `low`, and `high`.

#### Example Command for `acquire`

```txt
  acquire\n
  start_range 1\n
  end_range 5\n
  \n
```

#### Command Response for `acquire`

```txt
  200 OK\n
  settings-cookie 3\n
  Feedback for 'acquire':\n
  Clear transmit enable in frame header until done raising 150 volts.\n
  Limiting to frame_period=73880 (13.54 fps); requested=66666 (15.00 fps)\n
  Applying settings.\n
  \n

```

**Note:**
> There may be the occasional feedback from the ARIS that represents work-in-progress on the ARIS. For example, `Clear transmit enable...` above does not refer to your command, but a process used to bring up imaging for the first time. If you see feedback that doesn't appear to relate to your command, you can probably ignore it.

#### Environmental Effects

Water temperature, depth, and salinity have an effect on computed speed of sound which, in turn, affects parameters used in image acquisition. Client software indicates salinity during protocol initialization, but water temperature and depth can vary over time.

`acquire` uses the water temperature and depth at the time the `acquire` command is received to determine parameters for image acquisition. ARIS does not update those parameters if the measured water temperature or depth changes&mdash;to avoid modifying image acquisition settings during operations.

Note that water temperature and depth readings can change drastically between "on deck" and "in the water."

#### [More About Ranges]

#### [More about beams & tradeoffs]

| Model | Full beams | Half beams |
|-|-|-|
| ARIS 1200 | 48  | n/a |
| ARIS 1800 | 96  | 48  |
| ARIS 3000 | 128 | 64  |

#### [Gain]

#### [Frequency]

### [ARx Rotator Commands]

## Frame Protocol

In the simplified protocol, ARIS image data will be sent via UDP. One frame is broken up into multiple UDP datagrams, allowing us to adjust inter-datagram timing to accommodate slower network equipment. The payloads in multiple datagrams with the same frame index must be reassembled into a complete frame.

This table describes the header that starts the payload on each datagram. (All integral types are little-endian.)

| Field | Type | Offset | Description |
|-|-|-|-|
| `signature` | `uint32_t` | 0 | Contains the value `0x53495241` (little-endian; "ARIS"). |
| `header_size` | `uint32_t` | 4 | The size of this header, up to but not including `payload`. `payload`  follows the header immediately. |
| `frame_size` | `uint32_t` | 8 | This is the size of the ARIS frame header (1024 bytes) + the size of the frame's sample data. In other words, after you've reassembled the frame parts, this is 1024 + &lsaquo;total samples&rsaquo;. |
| `frame_index` | `uint32_t` | 12 | Identifies this frame. There are generally multiple datagrams per frame. If `frame_index` changes before the complete frame is received, the previous frame is incomplete. |
| `part_number` | `uint32_t` | 16 | A zero based index of the parts of the current frame. The first datagram's sequence number is 0. Frame's first datagram carries only the frame header (which is 1024 bytes), the second datagram's payload contains the first samples of the frame. |
| `payload_size` | `uint32_t` | 20 | The number of octets in the payload. |
| `payload` | `uint8_t[]` | 24 |  Payload bytes. The length of this field is the &lsaquo;datagram length&rsaquo; &thinsp;&ndash; `part_header_size`. |

Datagrams forming a very small frame could look those below, where there are 128 beams in the frame and 10 samples per beam. (10 is not a valid input.)

The first datagram contains the frame header in `payload` followed by 460 of the 1280 sample bytes. (Total sample count is `beams` &times; `samples_per_beam`.)

**Datagram 0** &mdash; 1500 bytes

| Field  | Offset | Value |
|-|-|-|
| `signature` | 0 | `0x53495241` |
| `header_size` | 4 | `20` |
| `frame_size` | 8 | 2304 *(1024 + [128 &times; 10])* |
| `frame_index` | 12 | `N` |
| `part_number` | 16 | `0` |
| `payload_size` | 20 | `1024` |

**Datagram 1** &mdash; 836 bytes

| Field | Offset | Value |
|-|-|-|
| `signature` | 0 | `0x53495241` |
| `header_size` | 4 | `20` |
| `frame_size` | 8 | 2304 *(1024 + [128 &times; 10])* |
| `frame_index` | 12 | `N` |
| `part_number` | 16 | `1` |
| `payload_size` | 20 | `M` |

> Note: frame indexes in ARIS and ARIS (`.aris`) recordings are numbered from 0, but are presented to users as if numbered from 1.

The ARIS frame header is defined [here](https://github.com/SoundMetrics/aris-file-sdk/blob/546a0fe948fa358eeab70b3238f1802552c3a6f8/type-definitions/C/FrameHeader.h#L16).
