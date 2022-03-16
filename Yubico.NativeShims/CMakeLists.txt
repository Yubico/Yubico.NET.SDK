cmake_minimum_required(VERSION 3.10)

project(Yubico.NativeShims VERSION 1.3.0)


if (APPLE)
    set(BACKEND "macscard")
elseif(UNIX)
    set(BACKEND "pcsc")
    find_package(PkgConfig REQUIRED)
elseif(WIN32)
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
    string(CONCAT CMAKE_SHARED_LINKER_FLAGS ${CMAKE_SHARED_LINKER_FLAGS}
        " /def:\"${CMAKE_CURRENT_SOURCE_DIR}/exports.msvc\"")
endif()


#
# Library dependencies
#

include(${CMAKE_SOURCE_DIR}/cmake/pcscd.cmake)
find_pcscd()


#
# Build definition
#
add_library(Yubico.NativeShims SHARED)

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
        )

# Linker
target_link_libraries(
    Yubico.NativeShims
        ${PCSC_LIBRARIES}
        ${PCSC_WIN_LIBS}
        ${PCSC_MACOSX_LIBS}
        ${PCSC_CUSTOM_LIBS}
        )