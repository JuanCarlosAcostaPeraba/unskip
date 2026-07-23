# Issue 23 working notes

- Windows Application events identified a `XamlParseException` when the first saved-device item was rendered.
- The local SQLite database remained present and valid; the failure was in presentation, not persistence.
- WPF attempted a source-updating binding from `Run.Text` to the read-only `ComputerName` property.
- Every display-only `Run.Text` binding now declares `Mode=OneWay`.
- A WPF regression test renders a main window containing a fictitious saved device on an STA thread.
- The test reproduced the production exception before the fix and passed after the binding correction.
- A packaged portable `0.1.1` candidate opened and closed normally against the existing current-user database without deleting local data.
