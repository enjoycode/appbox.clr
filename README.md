服务端C#工程, 由C++实现的主进程host clr运行时加载.

工程结构说明：
appbox.Core: 基础项目，包含模型定义、模型对应的数据结构、表达式定义、自定义序列化、缓存等；
appbox.Server: 服务端基础项目，主要包括通讯协议与存储api;
appbox.Design: IDE设计支持项目，主要处理前端IDE的各类命令;
appbox.Store: 存储项目，支持内置数据库及第三方数据库;
appbox.Host: 服务端主项目，引用上述项目，主要包含WebHost与前端通讯;
appbox.AppContainer: 服务端运行时的服务子进程，管理各服务模型的实例，通过共享内存与appbox.Host主进程通讯。
