export DOCKER_BUILDKIT=1

# We use Docker Build Kit as it supports advanced features such as
# cross-architecture building using QEMU, and extracting files from
# the final build image.
#
# Parameter guide:
#
# Tag: Right now, we do not save these images to any container
# registry. Because of this, it is fine that we're using a static
# version number in the tag. If building native shims become something
# we need to do on a more regular basis (like daily), we can look into
# caching the build environment to save on our CI runner workload.
#
# File: Points to the docker file definition that we wish to build.
# Each flavor of Linux should have their own Dockerfile to customize
# the build process to that particular distribution.
#
# Platform: The Docker platform identifier to build. The platforms
# we're interested in as part of the SDK are: linux/{amd64,386,arm64,arm/v7}
#
# Build-args: We pass in the hosts' user and group ID so that all files
# are ACL'd according to the host system.
#
# Output: We tell Docker to output the contents of the final image to
# the local fileystem, inside of the artifacts/{distro} directory.
# Within that folder, there should be a folder for each platform built.
# This will typically mean that there is a `linux` directory, followed
# by another folder for the processor architecture.
# For example: `artifacts/linux/386` for the 32-bit Linux build
#
# . : This tells Docker to use the current directory as the basis for
# the "context" to pass into the image using the COPY Dockerfile
# instruction. This should be the Yubico.NativeShims folder containing
# this script, as well as the CMakeLists.txt file.
#
# Extending this script:
#
# If we're adding a new build, say for a new distribution, we should
# simply add another docker buildx command. We need to add a call for
# each of the supported processor architectures (usually amd64 and arm64).
#
# We may want to consider refactoring this script to use functions at
# some point to make adding new distributions and architectures even
# easier.

# Distro: Ubuntu
# Arch: amd64/x64
# Output: ./ubuntu-x64/libYubico.NativeShims.so
docker buildx build \
    --tag yubico/nativeshims-ubuntu:1.0 \
    --file docker/Ubuntu/Dockerfile \
    --platform=linux/amd64 \
    --build-arg USER_ID=`id -u` \
    --build-arg GROUP_ID=`id -g` \
    --output type=local,dest=ubuntu-x64 \
    .

# Distro: Ubuntu
# Arch: i386/x86
# Output: ./ubuntu-x86/libYubico.NativeShims.so
docker buildx build \
    --tag yubico/nativeshims-ubuntu:1.0 \
    --file docker/Ubuntu/Dockerfile \
    --platform=linux/386 \
    --build-arg USER_ID=`id -u` \
    --build-arg GROUP_ID=`id -g` \
    --output type=local,dest=ubuntu-x86 \
    .

# Distro: Ubuntu
# Arch: arm64
# Output: ./ubuntu-arm64/libYubico.NativeShims.so
docker buildx build \
    --tag yubico/nativeshims-ubuntu:1.0 \
    --file docker/Ubuntu/Dockerfile \
    --platform=linux/arm64 \
    --build-arg USER_ID=`id -u` \
    --build-arg GROUP_ID=`id -g` \
    --output type=local,dest=ubuntu-arm64 \
    .
