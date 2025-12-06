@echo off
echo ========================================
echo GameWuxing 邮件验证码系统测试
echo ========================================

echo 步骤1: 编译项目
dotnet build src/GameEntry.csproj --no-dependencies

if %errorlevel% neq 0 (
    echo ❌ 编译失败
    pause
    exit /b 1
)

echo ✅ 编译成功

echo.
echo 步骤2: 检查日志输出
echo 请查看控制台输出，应该能看到：
echo - [Server] LoginServerHandler triggers registered successfully
echo - [Client] All response listeners registered successfully
echo - 发送验证码时的日志信息

echo.
echo 步骤3: 测试配置
if exist email.config.json (
    echo ✅ 发现邮件配置文件
    type email.config.json
) else (
    echo ⚠️ 未发现邮件配置文件，将使用模拟服务
    echo 如需测试真实邮件，请复制 email.config.example.json 为 email.config.json
    echo 并填写真实的SMTP配置信息
)

echo.
echo ========================================
echo 测试完成！
echo 请运行应用程序并测试邮件验证码功能
echo ========================================
pause