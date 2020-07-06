# RebootDeleter

Delete your files after system reboot.

# Usage

It's recommended to put the executable to `Program Files` folder and add to `%PATH%` environment variable.

`RebootDeleter.exe [[/reg [name]|/unreg]/[file1, file2, ...]]`

Delete the specified file.

`file: the file(s) to delete`

If you don't supply a filename, a open file dialog will pop up for you to choose.

`/reg [name]: Add the option "Delete on Reboot" to the context menu. If you supply a name, the context menu item will be named as it. Otherwise, translation in your computer's language will be used (if available).`

`/unreg: Remove the option "Delete on Reboot" from the context menu.`
