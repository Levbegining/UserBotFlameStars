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
            "api_id" => "23980583",
            "api_hash" => "5a54e4e3bb6db7aef474ad86ea27f30d",
            "phone_number" => "+79013310638",
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

        // === ДЕМО: управление конкретным ботом ===
        // 1) укажите юзернейм бота без '@'
        var botUsername = args.FirstOrDefault() ?? "flamestarsbot"; // пример
        var rp = await client.Contacts_ResolveUsername(botUsername); // peer бота
        // var botPeer = client.InputPeer(rp.peer);

        // 2) /start
        await client.SendMessageAsync(rp, "/start");
        Console.WriteLine("Отправил /start");
        await Task.Delay(1000);

        // // 3) Нажать кнопку обычной клавиатуры (ReplyKeyboard) — отправляем ее текст
        // // (Если ее нет — просто ничего не произойдет)
        // await PressReplyKeyboardButton(rp, "Menu");

        // // 4) Нажать Inline-кнопку с данным текстом (ищем последнюю сообщение с инлайн-клавой)
        // await PressInlineButton(rp, "Фарм звезд");

        await PressInlineButtonChain(rp, "✨ Фарм звезд");
        await Task.Delay(2000);
        await PressInlineButtonChain(rp, "✨ Фармить звёзды");
        await Task.Delay(90_003);

        while (true)
        {
            await PressInlineButtonChain(rp, "✨ Фармить звёзды");
            await Task.Delay(90_003);
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

    // === ReplyKeyboard: "нажать" = отправить текст кнопки
    private static async Task PressReplyKeyboardButton(InputPeer peer, string buttonText)
    {
        Console.WriteLine($"Пробую нажать ReplyKeyboard: \"{buttonText}\" (отправляю текст)...");
        await _client.SendMessageAsync(peer, buttonText);
    }

    // === InlineKeyboard: нажать callback-кнопку по тексту
    private static async Task PressInlineButton(InputPeer peer, string buttonText)
    {
        // Берем недавнюю историю чата и ищем последнюю Inline-клавиатуру
        var hist = await _client.Messages_GetHistory(peer, limit: 50); // :contentReference[oaicite:3]{index=3}
        var messages = hist.Messages.OfType<Message>()
                         .OrderByDescending(m => m.date);

        foreach (var m in messages)
        {
            if (m.reply_markup is not ReplyInlineMarkup rim) continue;

            foreach (var row in rim.rows)
                foreach (var b in row.buttons)
                {
                    switch (b)
                    {
                        case KeyboardButtonCallback kbc when
                            string.Equals(kbc.text, buttonText, StringComparison.OrdinalIgnoreCase):
                            {
                                Console.WriteLine($"Жму inline-кнопку: \"{kbc.text}\"");
                                // В MTProto это messages.getBotCallbackAnswer
                                var ans = await _client.Messages_GetBotCallbackAnswer(peer, m.ID, data: kbc.data);
                                Console.WriteLine($"Ответ callback: {(ans?.message ?? "(пусто/не текст)")}");
                                return;
                            }
                        case KeyboardButtonUrl url when
                            string.Equals(url.text, buttonText, StringComparison.OrdinalIgnoreCase):
                            {
                                Console.WriteLine($"Кнопка-URL: {url.url} (открывается в клиенте, по API «нажать» нельзя)");
                                return;
                            }
                        case KeyboardButtonSwitchInline sw when
                            string.Equals(sw.text, buttonText, StringComparison.OrdinalIgnoreCase):
                            {
                                Console.WriteLine("SwitchInline-кнопка переводит в inline-режим — «нажать» через API нельзя.");
                                return;
                            }
                    }
                }
        }

        Console.WriteLine("Inline-кнопка с таким текстом не найдена в последних сообщениях.");
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
                                        // Пытаемся получить ответ, но безопасно обрабатываем таймаут
                                        var ans = await _client.Messages_GetBotCallbackAnswer(peer, m.ID, data: kbc.data);
                                        Console.WriteLine($"Ответ: {(ans?.message ?? "(пусто/нет текста)")}");
                                    }
                                    catch (TL.RpcException ex) when (ex.Code == 400 && ex.Message.Contains("BOT_RESPONSE_TIMEOUT"))
                                    {
                                        Console.WriteLine("⚠️ Бот не ответил на callback (timeout). Продолжаем...");
                                    }

                                    // Небольшая задержка, чтобы бот успел прислать новые сообщения
                                    // await Task.Delay(delayInMs);
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
