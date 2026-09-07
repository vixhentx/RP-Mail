# RP-Mail

`RP-Mail` 是一款以模板替换和批量发送为核心的邮件发送客户端.

- `RPMailConsole`: 纯终端版本, 通过`json`来配置发送任务.
- `RPMailUI`: 图形化版本, 通过图形化界面配置发送任务, 并且可以独立导出各模块配置.

## 配置说明

### 内容配置

内容配置如下表所示, 关于"支持模板"的描述, 请看"模板渲染"部分.

| 项 | 格式 | 描述 | 支持模板 |
| -- | --- | --- | --- |
| 正文 | HTML文件 | 邮件的正文部分 | scriban |
| 主题 | 文本 | 邮件的主题(标题)部分 | scriban |
| 附件 | 多个 "文件 -> 名称" 的映射 | 邮件的附件部分 | typst + scriban |
| 额外属性表 | 多个 "键 -> 值" 的映射 | 为模板渲染提供的更多属性集 | - |
| 用户属性表 | CSV文件 | 为模板渲染和邮件发送提供的用户属性数据 | - |
| 字符集 | 单选项 | **全部**内容采用的文本编码 | - |

### 发件配置

发件配置与常见的`SMTP`配置含义相同.

- 发件邮箱
- 密码: SMTP密码
- 主机: SMTP主机, 支持附加端口.

### 输出配置

输出设置用来控制转换输出和发件的行为.

- 输出目录
- 发送后删除
- 仅转换不发送
- 保留原始文档
- 保存HTML

## 模板渲染

模板渲染采用`scriban`与`typst`.

- `Scriban`: 内容文本直接替换.
- `Typst`: 附件中的`PDF`生成.

### Scriban

scriban作为内容替换引擎使用. 通常我们使用 `{{ property }}` 的格式自动替换即可. [Scriban语法参考](https://scriban.github.io/docs/language/)

可用属性如下:

#### 用户属性(csv):
通过csv表格形式提供的用户属性集, 会在模板中作为user.XXX属性可用.

约定属性如下:
- `email`: 用户的邮箱地址. 会在发送时使用.

比如这样的一个csv文件:
```csv
email,name,group
l@yy.com,Yy,Software
zz@xx.xyz,Zz,Algorithm
```

会注入可用属性:
- user.email
- user.name
- user.group

#### 额外属性
额外属性为用户无关的全局属性, 以键值对的形式存在, 会自动注入到模板中.

这些属性名称不可用:
- user
- users

比如定义这些属性:
```
company -> RobotPilots
session -> 2026
```

会注入可用属性:
- company
- session

### 附件转换

附件转换功能允许将发送的附件与本地附件文件名不相同, 并提供了条件添加附件, pdf生成功能.

附件映射关系中的文本可以被`Scriban`解析替换, 解析后可以实现:

1. 动态映射关系:
    比如:
    ```text
    算法组.png -> 欢迎{{ user.name }}加入算法组.png
    ```
    或者:
    ```text
    {{ session }}/赛季招新海报.pdf -> 海报.pdf
    ```

2. 条件附件:
    如果映射结果为空串, 则不会添加这个附件.
    比如:
    ```text
    {{ user.group }}组.png -> {{ user.group == "电控组" ? "电控QQ群.png" : ""}}
    ```
    只有在"电控组"才会添加这个附件

3. Typst渲染:

    Typst用于pdf附件生成.

    [Typst语法参考](https://typst.app/docs/), 当然, 也可以让AI编写.

    附件中如果文件为`typ`后缀, 附件名为`pdf`后缀, 则会进行typst渲染:

    比如:
    ```text
    录取通知.typ -> {{ user.name }}录取通知.pdf
    ```

    则进行:
    ```mermaid
    flowchart LR
    File["模板文件"] --> Sbn["Scriban替换模板内容"] --> Typ["Typst渲染"] --> Pdf["最终PDF附件"]
    ```

