# BlitzText Windows 0.5.1

BlitzText 0.5.1 fixes an intermittent shutdown symptom caused by multiple app instances running at the same time.

## Fixes

- Prevents more than one BlitzText instance from running in the current Windows session.
- Restores and activates the existing BlitzText window when the app is launched again.
- Releases the single-instance lock during a normal shutdown so BlitzText can be started again immediately.

The MSI requires the .NET 8 Desktop Runtime.
