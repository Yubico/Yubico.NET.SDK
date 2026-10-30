//go:build !linux && !darwin && !freebsd && !openbsd && !netbsd && !dragonfly

package main

// Other platforms need host-managed crash-dump and debugger policy.
func hardenProcess() error { return nil }
