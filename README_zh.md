<div align="center">
  <p>
    <a href="README.md">English</a> | <a href="README_zh.md">简体中文</a>
  </p>
  <h1>CANopenEditor2</h1>
</div>

本项目复刻自 [CANopenEditor](https://github.com/CANopenNode/CANopenEditor.git)。
原始的 CANopenEditor 是 [libedssharp](https://github.com/robincornelius/libedssharp) 的一个分支，原作者为 Robin Cornelius。

CANopen 对象字典编辑器 (Object Dictionary Editor):

- 导入：支持 EDS 或 XDD 格式的 CANopen 电子数据文档。
- 导出：支持 EDS 或 XDD 格式的 CANopen 电子数据文档、文档说明、CANopenNode C 源代码文件等。
- 界面：提供 CANopen 对象字典、设备信息等图形界面(GUI)编辑器。提供用于简单转换的命令行(CLI)客户端。

CANopen 是一种国际标准化 (EN 50325-4) ([CiA301](https://can-cia.org/cia-groups/technical-documents)) 的高级协议，建立在 CAN 总线之上，用于嵌入式控制系统。关于 CANopen 的更多信息，请访问 <http://www.can-cia.org/。>

[CANopenNode](https://github.com/CANopenNode/CANopenNode) 是一个免费且开源的 CANopen 协议栈。

代码库结构
--------

本代码库包含三个主要项目：

- [LibEDSsharp](libEDSsharp/README.md)，一个用于操作 EDS 文件的 C# 核心库。它已经合并到上游，目前在此代码库中持续维护。
- [CLI (命令行工具)](EDSSharp/README.md)，用于在所有支持的格式之间进行简单的转换。
- [GUI (图形界面)](EDSEditorGUI2/README.md)，用于全面操作和编辑您的 CANopen 文件，提供现代化的跨平台界面。

如何使用
--------

1. [下载对应您操作系统 (Windows、macOS 或 Linux) 的最新发布版](https://github.com/CANopenNode/CANopenEditor/releases)。**请不要直接下载源码**。
2. 解压缩下载的文件。
3. 如果您使用 Windows，请直接运行 `CANopenEditor-Setup.exe` 进行安装，或解压便携版 `.zip` 后直接运行独立 `.exe`。
4. 如果您使用 macOS/Linux，请解压 `.tar.gz` 压缩包，然后运行对应操作系统的可执行文件。

支持的格式
--------

以下是迄今为止该库支持的格式完整列表，按类别排序：<br>

### CAN in Automation 官方格式

| 描述                                  | 导出器                                                     | 格式   |
|---------------------------------------|------------------------------------------------------------|--------|
| 电子数据文档 (CiA 306-1)              | ElectronicDataSheet                                        | .eds   |
| 设备配置文件 (CiA 306-1)              | DeviceConfigurationFile                                    | .dcf   |
| XML 设备描述文件 (CiA 311)            | CanOpenXDDv1.0<br>CanOpenXDDv1.1<br>CanOpenXDDv1.1stripped | .xdd   |
| XML 设备配置文件 (CiA 311)            | CanOpenXDCv1.1                                             | .xdc   |

### 扩展格式

| 描述                             | 导出器                                      | 格式   |
|----------------------------------|---------------------------------------------|--------|
| 网络 XML 设备描述文件            | CanOpenNetworkv1.0<br>CanOpenNetworkXDDv1.1 | .nxdd  |
| 网络 XML 设备配置文件            | CanOpenNetworkXDCv1.1                       | .nxdc  |
| XML Profile 描述文件             | 无 (None)                                   | .xpd   |

### CANopenEditor 专用格式

| 描述                                     | 导出器                                                   | 格式            |
|------------------------------------------|----------------------------------------------------------|-----------------|
| CANopen 项目文件                         | CanOpenProject                                           | .cpj                 |

### CANopenNode 专用格式

| 描述                                     | 导出器                                                   | 格式            |
|------------------------------------------|----------------------------------------------------------|-----------------|
| CanOpenNode 对象字典文件对               | CanOpenNode<br>CanOpenNodeV4                             | .h,.c                |
| PCanOpenNode 项目文件                    | CanOpenNodeProtobuf(json)<br>CanOpenNodeProtobuf(binary) | .json<br>.binpb      |

### 文档格式

| 导出器              | 格式   |
|---------------------|--------|
| DocumentationHTML   | .html  |
| DocumentationMarkup | .md    |
| NetworkPDOReport    | .md    |

文件结构
--------

您需要了解的主要文件和目录包括：

- [setup.nsi](setup.nsi) 是 Windows 安装脚本。
- [Makefile](Makefile) 是 Linux 安装和操作脚本。
- [EDSEditorGUI](EDSEditorGUI) 目录是旧版 GUI。功能完备但仅支持 Windows (已弃用)。
- [EDSEditorGUI2](EDSEditorGUI2) 目录是新版跨平台 GUI。提供现代化的界面，且原生完美支持 Windows、Mac 和 Linux 等操作系统。
- [EDSSharp](EDSSharp) 目录是 CLI 命令行工具。目前主要用于简单的格式转换。
- [GUITests](GUITests) 目录包含所有 GUI 单元测试。
- [Images](Images) 目录包含文档中使用的所有图片。
- [Tests](Tests) 目录包含所有库(Lib)相关的核心单元测试。
- [libEDSsharp](libEDSsharp) 目录包含了由 Robin Cornelius 编写的核心驱动和解析库。

近期修复 (Recent Fixes)
--------

- 修复了在导出 XDD 时，CANopen `VAR` 对象 (`OdObject`) 丢失 `DataType`、`Access`、`DefaultValue` 和 `ActualValue` 核心属性的严重 Bug。
- 修复了在加载/保存项目时，`lastModificationTime` 和 `createTime` 等时间戳无法正确解析并格式化为 ISO 8601 标准的问题。
- 修复了新版 GUI (`EDSEditorGUI2`) 在加载项目时由于 `OdObject` 和 `OdSubObject` 映射配置不严谨而触发 `AutoMapperConfigurationException` 导致崩溃的问题。
- 更新并验证了跨平台 (Linux) 的发布编译能力。

BUG 反馈
--------

如果您发现了任何 Bug，请在 GitHub 上提交 bug report，并附上您创建或打开的相关复现文件。我们非常需要您的帮助，主要的维护者们非常活跃并且会尽快回复您。

推荐使用免费的 [EDSchecker](https://www.vector.com/de/de/support-downloads/download-center/#product=%5B%2274771%22%5D&tab=1&pageSize=15&sort=date&order=desc) 工具来检查您的 EDS/XDD 文件是否合规。

参与贡献
--------

如果您想为这个项目做出贡献，首先向您表示感谢！请阅读我们的 [贡献指南 (CONTRIBUTING.md)](CONTRIBUTING.md)。我们对新手非常友好，即便您没有太多参与开源项目的经验，也请勇敢尝试！

核心成员 (Collaborators)
--------
<!-- readme: collaborators -start -->
<table>
	<tbody>
		<tr>
            <td align="center">
                <a href="https://github.com/jiujiujiur0000">
                    <img src="https://avatars.githubusercontent.com/u/95092734?v=4" width="100;" alt="jiujiujiur0000"/>
                    <br />
                    <sub><b>jiujiujiur0000</b></sub>
                </a>
            </td>
		</tr>
	<tbody>
</table>
<!-- readme: collaborators -end -->

贡献者 (Contributors)
--------
<!-- readme: contributors -start -->
<table>
	<tbody>
		<tr>
            <td align="center">
                <a href="https://github.com/robincornelius">
                    <img src="https://avatars.githubusercontent.com/u/159000?v=4" width="100;" alt="robincornelius"/>
                    <br />
                    <sub><b>robincornelius</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/jiujiujiur0000">
                    <img src="https://avatars.githubusercontent.com/u/95092734?v=4" width="100;" alt="jiujiujiur0000"/>
                    <br />
                    <sub><b>jiujiujiur0000</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/trojanobelix">
                    <img src="https://avatars.githubusercontent.com/u/15106425?v=4" width="100;" alt="trojanobelix"/>
                    <br />
                    <sub><b>trojanobelix</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/nimrof">
                    <img src="https://avatars.githubusercontent.com/u/9848846?v=4" width="100;" alt="nimrof"/>
                    <br />
                    <sub><b>nimrof</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/CANopenNode">
                    <img src="https://avatars.githubusercontent.com/u/13575344?v=4" width="100;" alt="CANopenNode"/>
                    <br />
                    <sub><b>CANopenNode</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/martinwag">
                    <img src="https://avatars.githubusercontent.com/u/676672?v=4" width="100;" alt="martinwag"/>
                    <br />
                    <sub><b>martinwag</b></sub>
                </a>
            </td>
		</tr>
		<tr>
            <td align="center">
                <a href="https://github.com/simon-fuchs-inmach">
                    <img src="https://avatars.githubusercontent.com/u/57712038?v=4" width="100;" alt="simon-fuchs-inmach"/>
                    <br />
                    <sub><b>simon-fuchs-inmach</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/reza0310">
                    <img src="https://avatars.githubusercontent.com/u/70545529?v=4" width="100;" alt="reza0310"/>
                    <br />
                    <sub><b>reza0310</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/heliochronix">
                    <img src="https://avatars.githubusercontent.com/u/1733202?v=4" width="100;" alt="heliochronix"/>
                    <br />
                    <sub><b>heliochronix</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/Bartimaeus-">
                    <img src="https://avatars.githubusercontent.com/u/2954254?v=4" width="100;" alt="Bartimaeus-"/>
                    <br />
                    <sub><b>Bartimaeus-</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/cfrank-mir">
                    <img src="https://avatars.githubusercontent.com/u/284268463?v=4" width="100;" alt="cfrank-mir"/>
                    <br />
                    <sub><b>cfrank-mir</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/JuPrgn">
                    <img src="https://avatars.githubusercontent.com/u/20264907?v=4" width="100;" alt="JuPrgn"/>
                    <br />
                    <sub><b>JuPrgn</b></sub>
                </a>
            </td>
		</tr>
		<tr>
            <td align="center">
                <a href="https://github.com/gotocoffee1">
                    <img src="https://avatars.githubusercontent.com/u/26260677?v=4" width="100;" alt="gotocoffee1"/>
                    <br />
                    <sub><b>gotocoffee1</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/wilkinsw">
                    <img src="https://avatars.githubusercontent.com/u/10655771?v=4" width="100;" alt="wilkinsw"/>
                    <br />
                    <sub><b>wilkinsw</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/pettaa123">
                    <img src="https://avatars.githubusercontent.com/u/31046837?v=4" width="100;" alt="pettaa123"/>
                    <br />
                    <sub><b>pettaa123</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/henrikbrixandersen">
                    <img src="https://avatars.githubusercontent.com/u/1076226?v=4" width="100;" alt="henrikbrixandersen"/>
                    <br />
                    <sub><b>henrikbrixandersen</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/ckhardin">
                    <img src="https://avatars.githubusercontent.com/u/1160137?v=4" width="100;" alt="ckhardin"/>
                    <br />
                    <sub><b>ckhardin</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/Regelink">
                    <img src="https://avatars.githubusercontent.com/u/1665817?v=4" width="100;" alt="Regelink"/>
                    <br />
                    <sub><b>Regelink</b></sub>
                </a>
            </td>
		</tr>
		<tr>
            <td align="center">
                <a href="https://github.com/Sl-Alex">
                    <img src="https://avatars.githubusercontent.com/u/7002691?v=4" width="100;" alt="Sl-Alex"/>
                    <br />
                    <sub><b>Sl-Alex</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/rgruening">
                    <img src="https://avatars.githubusercontent.com/u/72022918?v=4" width="100;" alt="rgruening"/>
                    <br />
                    <sub><b>rgruening</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/Barzello">
                    <img src="https://avatars.githubusercontent.com/u/52344726?v=4" width="100;" alt="Barzello"/>
                    <br />
                    <sub><b>Barzello</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/rcolatobe">
                    <img src="https://avatars.githubusercontent.com/u/86854948?v=4" width="100;" alt="rcolatobe"/>
                    <br />
                    <sub><b>rcolatobe</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/kekiefer">
                    <img src="https://avatars.githubusercontent.com/u/48104?v=4" width="100;" alt="kekiefer"/>
                    <br />
                    <sub><b>kekiefer</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/zhanglongqi">
                    <img src="https://avatars.githubusercontent.com/u/956693?v=4" width="100;" alt="zhanglongqi"/>
                    <br />
                    <sub><b>zhanglongqi</b></sub>
                </a>
            </td>
		</tr>
		<tr>
            <td align="center">
                <a href="https://github.com/DaMutz">
                    <img src="https://avatars.githubusercontent.com/u/406081?v=4" width="100;" alt="DaMutz"/>
                    <br />
                    <sub><b>DaMutz</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/StormOli">
                    <img src="https://avatars.githubusercontent.com/u/4819887?v=4" width="100;" alt="StormOli"/>
                    <br />
                    <sub><b>StormOli</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/possibly-not">
                    <img src="https://avatars.githubusercontent.com/u/12588174?v=4" width="100;" alt="possibly-not"/>
                    <br />
                    <sub><b>possibly-not</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/KwonTae-young">
                    <img src="https://avatars.githubusercontent.com/u/10510127?v=4" width="100;" alt="KwonTae-young"/>
                    <br />
                    <sub><b>KwonTae-young</b></sub>
                </a>
            </td>
		</tr>
	<tbody>
</table>
<!-- readme: contributors -end -->
