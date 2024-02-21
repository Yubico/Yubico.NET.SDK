# Base Image: Use Ubuntu 20.04 as the starting point for the build environment which ensures broad compatibility
FROM ubuntu:20.04 AS env

# Arguments: These are external variables that can be passed into the Docker build process (e.g. --build-arg USER_ID=1000)
ARG USER_ID
ARG GROUP_ID
ARG ARTIFACT_DIR

# Environment Variables: Set the PATH environment variable to include /usr/local/bin as well, this is where OpenSSL is installed
ENV PATH=/usr/local/bin:$PATH

# User and Group Setup: Create a new user and group with specified IDs to match the host's user for file permissions
# Package Installation: Update package lists and install necessary tools for building the application
RUN groupadd -f -g ${GROUP_ID} local && \
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
        libpcsclite-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/* && \
    # Install Kitware CMake: Add Kitware's repository for a newer version of CMake than what's available in Ubuntu's default repos
    (test -f /usr/share/doc/kitware-archive-keyring/copyright || \
    wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | \
    tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null) && \
    echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | \
    tee /etc/apt/sources.list.d/kitware.list >/dev/null && \
    apt-get update -qq && \
    apt-get install cmake -yq

# OpenSSL Installation: Download, compile, and install OpenSSL from source, updating library cache to include OpenSSL
RUN wget https://www.openssl.org/source/openssl-3.0.2.tar.gz && \
    tar -xzf openssl-3.0.2.tar.gz && \
    cd openssl-3.0.2 && \
    ./Configure zlib && \
    make && \
    # make test && \
    make install && \
    ldconfig /usr/local/lib64/ && \
    cd .. && \
    rm -rf openssl-3.0.2 openssl-3.0.2.tar.gz

# Development Stage: Setup the working directory for building the application
FROM env AS devel
WORKDIR /home/build

# Copy the entire context (source code) into the image
COPY . .

# Cleanup: Remove any pre-existing artifacts directory to ensure a clean build environment
RUN rm -rf artifacts

# Build Stage: Compile the Yubico.NativeShims shared object using CMake and store the outputs in an artifact directory
FROM devel AS build
RUN cmake -S . -B build_out -DCMAKE_BUILD_TYPE=Release && \
    cmake --build build_out --target all -v && \
    mkdir -p /home/build/artifacts/$ARTIFACT_DIR && \
    cp /home/build/build_out/*.so /home/build/artifacts/$ARTIFACT_DIR

# Final Image: Create a minimal final image containing only the build artifacts
FROM scratch AS build_install

# Copy build artifacts from the previous stage into the scratch image
COPY --from=build /home/build/artifacts/$ARTIFACT_DIR/ .
