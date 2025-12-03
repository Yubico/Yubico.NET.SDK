cmake_minimum_required(VERSION 3.13)

# Set default version if not provided via command line
if(NOT DEFINED PROJECT_VERSION)
    set(PROJECT_VERSION "1.14.0")
endif()

# Set VCPKG manifest version to match project version
set(VCPKG_MANIFEST_VERSION ${PROJECT_VERSION})

project(Yubico.NativeShims VERSION ${PROJECT_VERSION})
include(CheckCCompilerFlag)

if (APPLE OR UNIX)
    if (APPLE)
        set(PLATFORM_MACOS true)
        set(BACKEND "macscard")
        add_link_options("-Wl,-exported_symbols_list,${CMAKE_CURRENT_SOURCE_DIR}/exports.llvm")
    elseif(UNIX)
        find_package(PkgConfig REQUIRED)
        set(PLATFORM_LINUX true)
        set(BACKEND "pcsc")
        add_link_options("-Wl,-z,relro,-z,now,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/exports.gnu")
    endif()
    if (CMAKE_C_COMPILER_ID STREQUAL "Clang" OR
        CMAKE_C_COMPILER_ID STREQUAL "AppleClang" OR
        CMAKE_C_COMPILER_ID STREQUAL "GNU")

        add_compile_options(-Wall -Wextra -Werror)
        add_compile_options(-Wformat -Wformat-nonliteral -Wformat-security)
        add_compile_options(-Wshadow)
        add_compile_options(-Wcast-qual)
        add_compile_options(-Wbad-function-cast)
        add_compile_options(-pedantic -pedantic-errors)
        add_compile_options(-fpic)
        add_compile_options(-O2)
        add_compile_definitions (-D_FORTIFY_SOURCE=2)
        add_link_options(-fpic)

        check_c_compiler_flag("-fstack-protector-all" HAVE_STACK_PROTECTOR_ALL)
        if (HAVE_STACK_PROTECTOR_ALL)
            message(STATUS "-fstack-protector-all support detected")
            add_compile_options(-fstack-protector-all)
            add_link_options(-fstack-protector-all)
        else ()
            check_c_compiler_flag("-fstack-protector" HAVE_STACK_PROTECTOR)
            if(HAVE_STACK_PROTECTOR)
                message(STATUS "-fstack-protector support detected")
                add_compile_options(-fstack-protector)
                add_link_options(-fstack-protector)
            else ()
                message(WARNING "No stack protection supported.")
            endif ()
        endif()
    elseif()
        message(WARNING "No compatible compiler found for setting additional security compiler flags.")
    endif()
elseif(WIN32)
    set(PLATFORM_WINDOWS true)
    set(BACKEND "winscard")
    add_link_options("/guard:cf" "/def:${CMAKE_CURRENT_SOURCE_DIR}/exports.msvc")
    add_compile_options("/GS" "/Gs")
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

# Add Windows resource file for version info
if(WIN32)
    target_sources(
        Yubico.NativeShims
        PRIVATE
            Yubico.NativeShims.rc
            )
endif()

# Linker
target_link_libraries(
    Yubico.NativeShims
        ${PCSC_LIBRARIES}
        ${PCSC_WIN_LIBS}
        ${PCSC_MACOSX_LIBS}
        ${PCSC_CUSTOM_LIBS}
        OpenSSL::Crypto
        )
