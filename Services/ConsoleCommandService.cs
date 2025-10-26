using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace TagPhotoAlbum.Server.Services;

public class ConsoleCommandService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsoleCommandService> _logger;

    public ConsoleCommandService(IServiceProvider serviceProvider, ILogger<ConsoleCommandService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("控制台命令服务已启动");
        _logger.LogInformation("输入 'help' 查看可用命令");
        Console.Write("> ");

        // 使用非阻塞的方式检查控制台输入
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Console.KeyAvailable)
                {
                    Console.Write("> ");
                    var input = await Task.Run(() => Console.ReadLine(), stoppingToken);

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    var command = input.Trim().ToLowerInvariant();

                    switch (command)
                    {
                        case "help":
                            ShowHelp();
                            break;
                        case "changepassword":
                        case "change-password":
                            await ChangePasswordAsync();
                            break;
                        case "exit":
                        case "quit":
                            _logger.LogInformation("正在退出控制台命令服务...");
                            return;
                        default:
                            Console.WriteLine($"未知命令: {command}");
                            Console.WriteLine("输入 'help' 查看可用命令");
                            break;
                    }
                }

                // 短暂等待，避免CPU占用过高
                await Task.Delay(100, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理控制台命令时发生错误");
            }
        }

        _logger.LogInformation("控制台命令服务已停止");
    }

    private void ShowHelp()
    {
        Console.WriteLine("可用命令:");
        Console.WriteLine("\thelp\t- 显示此帮助信息");
        Console.WriteLine("\tchangepassword\t- 修改用户密码");
        Console.WriteLine("\texit\t- 退出控制台命令服务");
        Console.WriteLine("\tquit\t - 退出控制台命令服务");
    }

    private async Task ChangePasswordAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Console.Write("请输入用户名: ");
            var username = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("用户名不能为空");
                return;
            }

            // 查找用户
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                Console.WriteLine($"用户 '{username}' 不存在");
                return;
            }

            Console.Write("请输入新密码: ");
            var newPassword = ReadPassword();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                Console.WriteLine("密码不能为空");
                return;
            }

            // 更新密码（这里使用明文存储，实际生产环境应该使用哈希）
            user.PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(newPassword)));
            context.Users.Update(user);
            await context.SaveChangesAsync();

            Console.WriteLine($"用户 '{username}' 的密码已成功修改");
            _logger.LogInformation("用户密码已修改 - 用户名: {Username}", username);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"修改密码时发生错误: {ex.Message}");
            _logger.LogError(ex, "修改用户密码时发生错误");
        }
    }

    private string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return password.ToString();
    }
}