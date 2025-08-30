using System;
using System.Linq;
using System.Threading.Tasks;
using TL;                 // классы MTProto
using WTelegram;         // клиент

class Program
{
    private static Client _client;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Telegram userbot via WTelegram ===");

        // Конфиг: из env-переменных, остальное - через консольный ввод
        string? Config(string what) => what switch
        {
            "api_id" => Environment.GetEnvironmentVariable("TELEGRAM_API_ID"),
            "api_hash" => Environment.GetEnvironmentVariable("TELEGRAM_API_HASH"),
            "phone_number" => Environment.GetEnvironmentVariable("TELEGRAM_PHONE"),
            // Для кода/пароля оставляем null => WTelegram спросит в консоли
            // "session_pathname" => "tg_userbot.session", // при желании можно задать путь
            _ => null
        };

        using var client = new Client(Config);
        _client = client;

        // Подписка на апдейты (новые сообщения, и т.п.)
        client.OnUpdates += OnUpdates;

        // Логин: библиотека сама спросит verification_code / password, если надо
        var me = await client.LoginUserIfNeeded();
        Console.WriteLine($"Логин выполнен: {me} (id {me.id})");  // :contentReference[oaicite:2]{index=2}

        var botUsername = args.FirstOrDefault() ?? "flamestarsbot"; // пример
        var rp = await client.Contacts_ResolveUsername(botUsername); // peer бота

        await client.SendMessageAsync(rp, "/start");
        Console.WriteLine("Отправил /start");
        await Task.Delay(1000);

        await PressInlineButtonChain(rp, "✨ Фарм звезд");
        await Task.Delay(2000);

        // Запускаем обе задачи параллельно
        var farmTask = RunFarmingLoop(rp);
        var dailyTask = RunDailyRewardLoop(rp);

        await Task.WhenAll(farmTask, dailyTask);
    }

    // 🔹 Цикл ежедневки (в определённое время суток)
    private static async Task RunDailyRewardLoop(InputPeer rp)
    {
        var waitOffset = TimeSpan.FromDays(1) + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1);




        // ЗДЕСЬ! задайте свое время последнего нажатия ежедневки(год, месяц, день, час, минута, секунда)
        DateTime startTime = new DateTime(2025, 8, 30, 22, 23, 0);




        var nextTargetTime = startTime + waitOffset;
        while (true)
        {
            var waitTime = nextTargetTime - DateTime.Now;
            Console.WriteLine($"🎁 Жду {waitTime.Days} days {waitTime.Hours} hours {waitTime.Minutes} minutes до ежедневки...");
            await Task.Delay(waitTime);

            try
            {
                Console.WriteLine("🎁 Пробую забрать ежедневку...");
                await PressInlineButtonChain(rp, "🎁 Ежедневка");
                nextTargetTime += waitOffset;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DailyRewardLoop] Ошибка: {ex.Message}");
            }
        }
    }


    private static async Task RunFarmingLoop(InputPeer rp)
    {
        while (true)
        {
            await PressInlineButtonChain(rp, "✨ Фармить звёзды");
            await Task.Delay(500);
            var resCaptcha = await TrySolveCaptcha(rp);
            if (resCaptcha != int.MinValue + 1)
                await _client.SendMessageAsync(rp, resCaptcha.ToString());

            await Task.Delay(89_500);
        }
    }

    private static Task OnUpdates(UpdatesBase updates)
    {
        foreach (var upd in updates.UpdateList)
        {
            if (upd is UpdateNewMessage { message: Message msg })
            {
                var from = msg.From?.ToString() ?? msg.peer_id?.ToString();
                Console.WriteLine($"[upd] {from}: {msg.message}");

                // Подсказка: можно анализировать msg.ReplyMarkup (reply/inline клавиатуры)
                if (msg.reply_markup is ReplyInlineMarkup rim)
                {
                    var texts = rim.rows.SelectMany(r => r.buttons)
                                        .Select(b => (b as KeyboardButtonBase)?.ToString());
                    Console.WriteLine("  (есть inline-кнопки)");
                }
            }
        }
        return Task.CompletedTask;
    }

    private static async Task<int> TrySolveCaptcha(InputPeer peer)
    {
        System.Console.WriteLine("Try solve captcha start...");
        var hist = await _client.Messages_GetHistory(peer, limit: 10);
        var m = hist.Messages.OfType<Message>().OrderByDescending(m => m.date).First();

        // foreach (var m in messages)
        // {
        if (m.reply_markup != null) return int.MinValue + 1;

        // var msg = m.message.Replace("", "");
        if (m.message.Length < 40 && m.message.StartsWith("Пожалуйста, решите пример: "))
        {
            var msg = m.message.Replace("Пожалуйста, решите пример: ", "");
            Console.WriteLine($"[TrySolveCaptcha] right message: {m.message}");
            Console.WriteLine($"[TrySolveCaptcha] trim message: {msg}");

            var expr = msg.Replace("=", "");
            int res = int.MinValue + 1;

            if (expr.Contains("+"))
            {
                var splitExpr = expr.Split("+");
                res = int.Parse(splitExpr[0]) + int.Parse(splitExpr[1]);
            }
            else if (expr.Contains("-"))
            {
                var splitExpr = expr.Split("-");
                res = int.Parse(splitExpr[0]) - int.Parse(splitExpr[1]);
            }
            else if (expr.Contains("*"))
            {
                var splitExpr = expr.Split("*");
                res = int.Parse(splitExpr[0]) * int.Parse(splitExpr[1]);
            }
            else if (expr.Contains("/"))
            {
                var splitExpr = expr.Split("/");
                res = int.Parse(splitExpr[0]) / int.Parse(splitExpr[1]);
            }
            else
            {
                System.Console.WriteLine("[TrySolveCaptcha] TrySolveCaptcha end(НЕ ТОТ ОПЕРАНД)!");
                return res;
            }

            System.Console.WriteLine("[TrySolveCaptcha] TrySolveCaptcha end(WITH CAPTCHA)!");
            return res;
        }
        // }
        System.Console.WriteLine("[TrySolveCaptcha] TrySolveCaptcha end(NO CAPTCHA)!");
        return int.MinValue + 1;
    }

    // Нажать inline-кнопку по тексту, и при желании сразу искать следующую
    private static async Task PressInlineButtonChain(InputPeer peer, params string[] buttonPath)
    {
        foreach (var targetText in buttonPath)
        {
            Console.WriteLine($"Ищу inline-кнопку: \"{targetText}\"...");

            var hist = await _client.Messages_GetHistory(peer, limit: 30);
            var messages = hist.Messages.OfType<Message>()
                             .OrderByDescending(m => m.date);

            bool pressed = false;

            foreach (var m in messages)
            {
                if (m.reply_markup is not ReplyInlineMarkup rim) continue;

                foreach (var row in rim.rows)
                    foreach (var b in row.buttons)
                    {
                        switch (b)
                        {
                            case KeyboardButtonCallback kbc
                                when string.Equals(kbc.text, targetText, StringComparison.OrdinalIgnoreCase):
                                {
                                    Console.WriteLine($"Жму inline-кнопку: \"{kbc.text}\"");

                                    try
                                    {
                                        _client.Messages_GetBotCallbackAnswer(peer, m.ID, data: kbc.data);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[FireAndForget] Ошибка: {ex.Message}");
                                    }

                                    pressed = true;
                                    break;
                                }

                            case KeyboardButtonUrl url
                                when string.Equals(url.text, targetText, StringComparison.OrdinalIgnoreCase):
                                {
                                    Console.WriteLine($"Кнопка-URL: {url.url} (API не может перейти, только вывести ссылку)");
                                    pressed = true;
                                    break;
                                }
                        }

                        if (pressed) break;
                    }

                if (pressed) break;
            }

            if (!pressed)
            {
                Console.WriteLine($"❌ Кнопка \"{targetText}\" не найдена.");
                return;
            }
        }

        Console.WriteLine("✅ Цепочка кнопок пройдена");
    }

}
