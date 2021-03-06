# Roadmap

This project is an assemblage of packages to be used in support of
controlling Sound Metrics devices, such as ARIS Explorer and Voyager.

This project currently has no timeline; the initial release likely will be
in support of internal tooling.

> **NOTE:** this project is currently in pre-release form. Unannounced breaking changes
> may occur at any time.

## Solution Structure

### ./

The root of the solution contains projects that build to packaged assemblies, to
be published on [nuget.org](https://nuget.org/).

### ./Solution Items

Various bits, including this file, that support the building, versioning,
and publishing of the packaged assemblies.

### ./test

Unit test and other projects used for proving functionality.

### ./tools

Tools in support of the packaged assemblies. E.g., `GetProtobufTools` retrieves
a protobuf package used in the build process.

### ./WPF

Projects in support of WPF visualization of device images. Presently unsupported.
