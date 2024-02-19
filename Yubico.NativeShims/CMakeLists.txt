cmake_minimum_required(VERSION 3.10)

project(Yubico.NativeShims VERSION 1.3.0)

if (APPLE)
    set(PLATFORM_MACOS true)
    set(BACKEND "macscard")
elseif(UNIX)
    set(PLATFORM_LINUX true)
    set(BACKEND "pcsc")
    find_package(PkgConfig REQUIRED)
elseif(WIN32)
    set(PLATFORM_WINDOWS true)
    set(BACKEND "winscard")
endif()

# export list
if(APPLE AND (CMAKE_C_COMPILER_ID STREQUAL "Clang" OR
   CMAKE_C_COMPILER_ID STREQUAL "AppleClang"))
    # clang + lld
    string(CONCAT CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS}
        " -exported_symbols_list ${CMAKE_CURRENT_SOURCE_DIR}/exports.llvm")
elseif(NOT MSVC)
    # clang/gcc + gnu ld
    string(CONCAT CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS}
        " -Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/exports.gnu")
else()
#enable control flow guard for windows
    string(CONCAT CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS}
        " /guard:cf /def:\"${CMAKE_CURRENT_SOURCE_DIR}/exports.msvc\"")
    string(CONCAT CMAKE_EXE_LINKER_FLAGS ${CMAKE_EXE_LINKER_FLAGS}
        " /guard:cf /def:\"${CMAKE_EXE_LINKER_FLAGS}/exports.msvc\"")
endif()


#
# Library dependencies
#

include(${CMAKE_SOURCE_DIR}/cmake/pcscd.cmake)
find_pcscd()

find_package(OpenSSL REQUIRED)

#
# Build definition
#
add_library(Yubico.NativeShims SHARED)

# Enable IPO/LTO (Link time optimization) if supported
# Optional IPO. Do not use IPO if it's not supported by compiler.
include(CheckIPOSupported)

check_ipo_supported(RESULT result OUTPUT output)
if(result)
    message(INFO "IPO is enabled.")
    set_property(TARGET Yubico.NativeShims PROPERTY INTERPROCEDURAL_OPTIMIZATION TRUE)
else()
    message(WARNING "IPO is not supported: ${output}")
endif()

# Pre-processor
configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/Yubico.NativeShims.h.in
    ${CMAKE_CURRENT_SOURCE_DIR}/Yubico.NativeShims.h
    @ONLY
    )

target_include_directories(
    Yubico.NativeShims
    PUBLIC
        "${PROJECT_BINARY_DIR}"
        )

# Source
target_sources(
    Yubico.NativeShims
    PRIVATE
        pcsc.c
        ssl.bignum.c
        ssl.ecgroup.c
        ssl.ecpoint.c
        ssl.gcmevp.c
        ssl.cmac.c
        )

# Linker
target_link_libraries(
    Yubico.NativeShims
        ${PCSC_LIBRARIES}
        ${PCSC_WIN_LIBS}
        ${PCSC_MACOSX_LIBS}
        ${PCSC_CUSTOM_LIBS}
        OpenSSL::Crypto
        )
