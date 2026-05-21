## 终末地实时坐标显示  
### 介绍  
> 该工具以森空岛地图工具API制作，能够打开一个窗口，实时显示玩家在终末地的坐标，方便玩家在终末地中导航。  

## 用法
1. 下载`endfield-player-position-display.exe`可执行文件，放在任意目录  
2. 在相同目录新建`token.txt`文本文件  
3. 打开[森空岛官网](https://www.skland.com/)并登录  
4. 登录完成后打开[这个网址](https://web-api.skland.com/account/info/hg)，然后复制`content`后面引号内的字符串（不包含引号），填入`token.txt`中（注意此`token`不要泄漏给别人）  
5. 打开`endfield-player-position-display.exe`，正常情况下即可看到玩家在终末地的坐标显示了。  

## FAQ  
token具体是什么？  
> 假设你打开页面后，显示的内容是：
```
{"code":0,"data":{"content":"ABCDEFGHIJKLMNOPQRST"},"msg":"接口会返回您的鹰角网络通行证账号的登录凭证，此凭证可以用于鹰角网络账号系统校验您登录的有效性。泄露登录凭证属于极度危险操作，为了您的账号安全，请勿将此凭证以任何形式告知他人！"}
```
> 那么你的token就是`ABCDEFGHIJKLMNOPQRST`这串字符串。  

提示“请同意获取角色位置的相关政策后重试”时怎么办  
>先在森空岛打开[地图工具](https://game.skland.com/map/endfield)，在右下角打开“位置同步”，按森空岛要求操作，直到可以在地图工具内正确显示自己的位置后关闭并重新打开本工具  

## 说明
> 本工具是整理框架后由AI生成代码的。  