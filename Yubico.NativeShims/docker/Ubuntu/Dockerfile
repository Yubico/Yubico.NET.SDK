# Base Image
FROM ubuntu:20.04 AS env

# Arguments
ARG USER_ID
ARG GROUP_ID
ARG ARTIFACT_DIR

# Set environment variables for the build
# - Include /usr/local/bin in PATH for easy access to installed tools
# - Define CMAKE_TOOLCHAIN_FILE to integrate with vcpkg
# - Enable VCPKG_FORCE_SYSTEM_BINARIES for compatibility with arm builds (See: https://stackoverflow.com/questions/64455340/vcpkg-force-system-binaries-errors-when-running-vcpkg-on-raspberry-pi)
ENV PATH=/usr/local/bin:$PATH \
    CMAKE_TOOLCHAIN_FILE=/vcpkg/scripts/buildsystems/vcpkg.cmake \
    VCPKG_FORCE_SYSTEM_BINARIES=1

# Install dependencies, tools, and set up the user
# This section combines updates, installs, and cleanup to keep the image size small
RUN set -x && groupadd -f -g ${GROUP_ID} local && \
    useradd -u ${USER_ID} -g ${GROUP_ID} -s /bin/sh local && \
    apt-get update -qq && \
    DEBIAN_FRONTEND=noninteractive apt-get install -yq \
        wget \
        ca-certificates \
        gnupg \
        software-properties-common \
        build-essential \
        pkg-config \
        ninja-build \
        zlib1g-dev \
        libpcsclite-dev \
        git \
        curl \
        zip \
        unzip \
        tar && \
    # Install Kitware CMake to get the latest CMake version for Ubuntu 20.04 published by Kitware (maintainer of CMake)
    (wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | \
    tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null) && \
    echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | \
    tee /etc/apt/sources.list.d/kitware.list >/dev/null && \
    apt-get update && \
    apt-get install cmake -yq && \
    # Clean up to reduce image size
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/* && \
    # Install vcpkg and dependencies. 
    # This package manager makes sure that all the correct and compatible build flags are used so that it can match the rest of the Yubico.NativeShims.dll build.
    git clone https://github.com/Microsoft/vcpkg.git /vcpkg && \
    /vcpkg/bootstrap-vcpkg.sh

# Setting up a development environment based on the previous stage
FROM env AS develop
WORKDIR /home/build
COPY . .
RUN rm -rf artifacts

# Build
FROM develop AS build
RUN cmake -S . -B build_out -DCMAKE_BUILD_TYPE=Release && \
    cmake --build build_out --target all -v && \
    mkdir -p /home/build/artifacts/$ARTIFACT_DIR && \
    cp /home/build/build_out/*.so /home/build/artifacts/$ARTIFACT_DIR

# Prepare the final image with just the build artifacts
FROM scratch AS build_install
COPY --from=build /home/build/artifacts/$ARTIFACT_DIR/ .
