# RebootDeleter
Delete your files after system reboot.

# Usage

It's recommended to put the executable to `Program Files` or `Windows` folder.

`RebootDeleter.exe [filename] [/reg [name]|/unreg]`

Delete the specified file.

`filename: the file to delete`

If you don't supply a filename, a open file dialog will pop up for you to choose.

`/reg [name]: Add the option "Delete on Reboot" to the context menu. If you supply a name, the context menu item will be named as it. Otherwise, translation in your computer's language will be used (if available).`

`/unreg: Remove the option "Delete on Reboot" from the context menu.`
