# RebootDeleter

在系统重启时删除文件。

# 使用方法

建议将二进制文件放置于 `Program Files` 文件夹并添加至 `%PATH%` 环境变量。

`RebootDeleter.exe [[/reg [name]|/unreg]/[file1, file2, ...]]`

删除指定的文件。

`file: 需要删除的文件`

如果您不提供文件名，一个选择文件的对话框将弹出以供选择。

`/reg [name]: 添加 "在重新启动时删除" 的选项到右键菜单中。如果您指定了 name，右键菜单项名称将被命名为此，否则将尝试使用翻译内容。`

`/unreg: 从右键菜单中移除。`
