# Shared macOS input implementation and platform requirements. The synthetic
# backend remains exclusive to the standalone regression harness.
set(HIDINPUT_COMMON_SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/../hidinput/owner.c
    ${CMAKE_CURRENT_LIST_DIR}/../hidinput/backend_iohid.c)
set(HIDINPUT_COMPILE_OPTIONS -fblocks)
set(HIDINPUT_FRAMEWORKS "-framework IOKit" "-framework CoreFoundation")
