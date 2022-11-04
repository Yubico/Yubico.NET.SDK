# We use Bionic as the basis for our image. Bionic is the last Ubuntu long-term support
# release that is both still supported and still supports i386 processors. Once Bionic
# leaves support, we can consider upgrading to the next LTS release.
FROM ubuntu:bionic AS env

# These arguments are passed in from the console or by Docker-CLI itself. User/Group ID
# is used to run the shell as the host's user identity. Artifact_dir should be set to
# the NuGet runtime-id for this platform (e.g. ubuntu-x64) to aid in artifact discovery
# in the GitHub Action workflow.
ARG USER_ID
ARG GROUP_ID
ARG ARTIFACT_DIR

# Set up basic environment variables such as the path.
ENV PATH=/usr/local/bin:$PATH

# Add the host user and group to the image.
RUN groupadd -f -g ${GROUP_ID} local && useradd -o -u ${USER_ID} -g ${GROUP_ID} -s /bin/sh local

# Install build tools
RUN apt-get update -qq \
&& DEBIAN_FRONTEND=noninteractive apt-get install -yq \
    wget \
    ca-certificates \
    gnupg \
    software-properties-common \
    build-essential \
    pkg-config \
    ninja-build \
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/* /tmp/* /var/temp/*

# Install latest CMake
# Ubuntu Bionic contains a very old version of CMake (3.10). Our project requires a newer
# version of the tool. This is the easiest way to update CMake using the official builds
# from Kitware (makers of CMake).
RUN apt-get update -qq \
&& wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | apt-key add - \
&& add-apt-repository 'deb https://apt.kitware.com/ubuntu/ bionic main' \
&& apt-get update -qq \
&& apt-get install cmake -yq \
&& cmake -version

# Install build dependencies
# This is where we should add any additional dependencies needed by Yubico.NativeShims.
# We could use vcpkg to help with dependencies, but for Linux, the distro's package
# manager is still almost always going to be the easiest way of finding the necessary
# headers and pre-built libraries. Be sure to use the -dev packages, as these typically
# denote the package that contains headers and libs.
RUN  apt-get update -qq \
&& apt-get install -yq \
    libpcsclite-dev \
    libssl-dev

# Snapshot the base environment. If we ever decide to cache our images in a container
# registry, `env` is the target we'd want to capture. The dependencies will be installed
# but we have not yet copied the source code to build into the image. That happens in
# this (devel) stage.
FROM env AS devel
# Let's work out of a folder that's out of the way on the filesystem.
WORKDIR /home/build
# Copy the host context (source code) into the image. See the notes in the shell script
# that invokes Docker to see the other end of specifying the context. Copies all of the
# host context (recursively) into the current working dir in the Docker image.
COPY . .
RUN rm -rf artifacts

# Build the Yubico.NativeShims shared object
# Now we take the `devel` target, and fork another image for building. This way, we can
# quickly roll back a failed build and retry (or try interactively). Put all of the
# build instructions in this stage. For now, this simply means generating the CMake
# cache, and building using CMake. We move the build artifacts into a well known
# location to help the artifact stage.
FROM devel AS build
RUN cmake -S . -B build_out -DCMAKE_BUILD_TYPE=Release
RUN cmake --build build_out --target all -v
RUN mkdir -p /home/build/artifacts/$ARTIFACT_DIR \
&& cp /home/build/build_out/*.so /home/build/artifacts/$ARTIFACT_DIR

# Copy over the build artifacts to a blank image. This way we can easily retrieve the
# build results without copying all of the previous image's filesystem. `Scratch` is
# a completely blank image. We then use the `COPY` instruction to pull only the files
# we care about into this blank space. The `--output` argument to the `docker` command
# specifies what we do with this result.
FROM scratch AS build_install
COPY --from=build /home/build/artifacts/$ARTIFACT_DIR/ .
