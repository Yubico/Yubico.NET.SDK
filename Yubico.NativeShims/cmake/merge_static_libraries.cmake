cmake_minimum_required(VERSION 3.15)

foreach(required_variable
        MERGE_PLATFORM
        INPUT_ARCHIVE
        DEPENDENCY_ARCHIVE
        OUTPUT_ARCHIVE)
    if(NOT DEFINED ${required_variable} OR "${${required_variable}}" STREQUAL "")
        message(FATAL_ERROR "${required_variable} is required")
    endif()
endforeach()

if(NOT EXISTS "${INPUT_ARCHIVE}")
    message(FATAL_ERROR "NativeShims archive not found: ${INPUT_ARCHIVE}")
endif()
if(NOT EXISTS "${DEPENDENCY_ARCHIVE}")
    message(FATAL_ERROR "OpenSSL::Crypto archive not found: ${DEPENDENCY_ARCHIVE}")
endif()

get_filename_component(DEPENDENCY_EXTENSION "${DEPENDENCY_ARCHIVE}" EXT)
string(TOLOWER "${DEPENDENCY_EXTENSION}" DEPENDENCY_EXTENSION)
if(MERGE_PLATFORM STREQUAL "WINDOWS")
    if(NOT DEPENDENCY_EXTENSION STREQUAL ".lib")
        message(FATAL_ERROR "OpenSSL::Crypto must resolve to a static .lib archive: ${DEPENDENCY_ARCHIVE}")
    endif()
elseif(NOT DEPENDENCY_EXTENSION STREQUAL ".a")
    message(FATAL_ERROR "OpenSSL::Crypto must resolve to a static .a archive: ${DEPENDENCY_ARCHIVE}")
endif()

set(TEMP_ARCHIVE "${OUTPUT_ARCHIVE}.merge.tmp")
set(MRI_FILE "${OUTPUT_ARCHIVE}.merge.mri")
file(REMOVE "${TEMP_ARCHIVE}" "${MRI_FILE}")

if(MERGE_PLATFORM STREQUAL "APPLE")
    if(NOT DEFINED LIBTOOL OR "${LIBTOOL}" STREQUAL "")
        message(FATAL_ERROR "libtool is required to merge macOS archives")
    endif()
    execute_process(
        COMMAND
            "${LIBTOOL}" -static -o "${TEMP_ARCHIVE}"
            "${INPUT_ARCHIVE}" "${DEPENDENCY_ARCHIVE}"
        RESULT_VARIABLE merge_result
        ERROR_VARIABLE merge_error
        )
elseif(MERGE_PLATFORM STREQUAL "LINUX")
    if(NOT DEFINED AR OR "${AR}" STREQUAL "")
        message(FATAL_ERROR "CMAKE_AR is required to merge Linux archives")
    endif()
    file(WRITE "${MRI_FILE}"
        "CREATE \"${TEMP_ARCHIVE}\"\n"
        "ADDLIB \"${INPUT_ARCHIVE}\"\n"
        "ADDLIB \"${DEPENDENCY_ARCHIVE}\"\n"
        "SAVE\n"
        "END\n")
    execute_process(
        COMMAND "${AR}" -M
        INPUT_FILE "${MRI_FILE}"
        RESULT_VARIABLE merge_result
        ERROR_VARIABLE merge_error
        )
    file(REMOVE "${MRI_FILE}")
elseif(MERGE_PLATFORM STREQUAL "WINDOWS")
    if(NOT DEFINED AR OR "${AR}" STREQUAL "")
        message(FATAL_ERROR "CMAKE_AR is required to merge Windows archives")
    endif()
    execute_process(
        COMMAND
            "${AR}" /NOLOGO "/OUT:${TEMP_ARCHIVE}"
            "${INPUT_ARCHIVE}" "${DEPENDENCY_ARCHIVE}"
        RESULT_VARIABLE merge_result
        ERROR_VARIABLE merge_error
        )
else()
    message(FATAL_ERROR "Unsupported archive merge platform: ${MERGE_PLATFORM}")
endif()

if(NOT merge_result EQUAL 0 OR NOT EXISTS "${TEMP_ARCHIVE}")
    file(REMOVE "${TEMP_ARCHIVE}" "${MRI_FILE}")
    message(FATAL_ERROR "Failed to merge static archives: ${merge_error}")
endif()

# The original archive remains intact unless the merge succeeds. Replace it only
# after the complete merged archive has been written to a temporary path.
file(REMOVE "${OUTPUT_ARCHIVE}")
file(RENAME "${TEMP_ARCHIVE}" "${OUTPUT_ARCHIVE}")
