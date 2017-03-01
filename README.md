# ARIS Integration SDK
This SDK supports connecting to and commanding an ARIS sonar.
For information on ARIS frame formats, see [ARIS File SDK](https://github.com/SoundMetrics/aris-file-sdk).

## Contents

### Documentation
The following documentation is provided:

**aris-integration-sdk/documents/ARIS-Integration-SDK.pdf**: Describes the mechanisms necessary to build an application to control an ARIS.

**aris-integration-sdk/documents/Integration SDK suggested settings.xlsx**: An Excel spreadsheet that provides rudimentary support in choosing valid acoustic settings.

Documentation of file formats and frame headers can be found in our repo `aris-file-sdk`.

### Common Code

**aris-integration-sdk/common/code/**: These folders contain the following:

- **FrameStream** - reference code to receive parts of images over the network and assemble them into complete frames.
- **Reordering** - sample code to reorder received frame parts; the reordering code should be treated as immutable reference code.
- **SpeedOfSound** - reference code to calculate the speed of sound from water temperature, salinity, and depth; includes references on such calculations.
- **Depth** - sample code to calculate depth from pressure, salinity, and temperature.
- **UpdateFrameHeader** - reference code for injecting values into ARISframe headers; this is not a common operation.

**aris-integration-sdk/sample-code/**: These folders contain the following:

- **connect-command** - sample program illustrating how to establish a connection to the ARIS and send commands.
- **reorder-frame** - sample program/unit test that illustrates reordering an ARIS frame; this makes use of the reordering code in aris-integration-sdk/common/code/.

## Exclusions
This SDK provides capabilities for controlling an ARIS Explorer. ARIS Defender cannot be controlled with this SDK, only the built-in supervisor functionality is available for ARIS Defender.

This SDK, beginning with version 2.0, supersedes a previous ARIS 1.x SDK. The older SDK is not compatible with current ARIS production.

## Git-specific Issues
This git repository includes the submodule `aris-file-sdk`. If you intend to clone this repo
you'll want to use the `--recursive` flag.


## Miscellaneous

### Obsolete
The focus maps in aris-integration-sdk/common/focus-maps/ are no longer necessary for new ARIS controller applications. In their place, please use the `focusRange` field of the `SetFocusPosition` messages to set the focus range in meters.

### Version Numbers
The stated version number of this SDK does not track with Sound Metrics' other software applications' minor version numbers.
