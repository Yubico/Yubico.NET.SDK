//go:build darwin || freebsd || openbsd || netbsd || dragonfly

package main

import "golang.org/x/sys/unix"

func hardenProcess() error {
	return unix.Setrlimit(unix.RLIMIT_CORE, &unix.Rlimit{})
}
