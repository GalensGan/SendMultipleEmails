# 开发说明

## 思路

1. 每位用户维护多个发件队列
2. 系统开辟多个线程，每个线程从各个用户的发件队列中取出一封邮件，轮流发送
3. 每个用户可设置限制，允许同时发送的任务数量