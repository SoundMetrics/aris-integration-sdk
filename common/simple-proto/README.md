# Simplified Protocol

## Caveats

This folder proposed a simplified protocol for commanding an ARIS. Until this reaches the `master` branch, this should be considered "not released." The targeted milestone is a future ARIScope 2.8 release.

## Command Format

The commands are a text-based, with the command name followed by 0 or more lines of key-value pairs. The command is terminated by a blank line.

For illustration, we're showing new lines as \n here:

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

### `init`

`init` is required every time a connection is made.

| Parameter | Description |
|-|-|
| `salinity` | Required. There are three valid values: `fresh`, `brackish`, and `saltwater`. Salinity affects the speed of sound in water, and affects pretty much every calculation involving time and distance. |
| `report` | Optional, false by default. Valid values are `true` and `false`. This controls whether the ARIS responds to commands with descriptive text, which may be useful during integration. This parameter may have little value after your integration is complete. |
| `datetime` | Setting the date and time should be considered mandatory by all clients. If the RTC were to fail, times would not be consistent across power cycles. The required format is `2017-Apr-01 13:24:35`; US English is assumed for the month abbreviation. See below for more on formatting this parameter. |
| `rcvrport` | Required. This is the port number on which you will receive frames. You should open your receive socket before connecting to the ARIS. |
| `rcvrip` | Optional. Specifies an IPv4 address in dotted format. E.g., `192.168.1.42`. If not provided, the ARIS will send frames to the host that opened the command connection. |

#### Formatting the `datetime` parameter

The following code illustrates formatting for the `datetime` parameter. Unfortunately, it is locale dependent and assumes US Englinsh. Specifically, the `%b` specifier may not be correct in some locales, and the ARIS expects US English for the month names.

```cpp
      struct tm now;
      char now_str[64];
      const time_t rawtime = time(NULL);
      localtime_s(&now, &rawtime);

      constexpr auto fmt = "%Y-%b-%d %T"; // 2019-Apr-01 13:24:35
      strftime(now_str, sizeof(now_str), fmt, &now);
```

### (more commands to come)

## Frame Format

