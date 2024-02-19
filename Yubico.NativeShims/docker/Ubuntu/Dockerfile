FROM ubuntu:20.04 AS env

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
RUN groupadd -f -g ${GROUP_ID} local && useradd -u ${USER_ID} -g ${GROUP_ID} -s /bin/sh local

# Install build tools
RUN apt-get update -qq \
&& DEBIAN_FRONTEND=noninteractive apt-get install -yq \
    wget \
    gpg \
    ca-certificates \
    gnupg \
    software-properties-common \
    build-essential \
    pkg-config \
    ninja-build \
    zlib1g-dev  \
    libpcsclite-dev \
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/* /tmp/* /var/temp/*

RUN test -f /usr/share/doc/kitware-archive-keyring/copyright || \
    wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null

RUN echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | tee /etc/apt/sources.list.d/kitware.list >/dev/null \
    apt-get update \
    apt-get install cmake

# Install build dependencies
RUN wget https://www.openssl.org/source/openssl-3.0.2.tar.gz \
    && tar -xzf openssl-3.0.2.tar.gz \
    && rm openssl-3.0.2.tar.gz

# Compile and install OpenSSL
RUN cd openssl-3.0.2 \
    && ./Configure --openssldir=/usr/local/ssl zlib \
    && make \
    && make install \
    && ldconfig /usr/local/lib64/

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
