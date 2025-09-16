## UserBotFlameStars — `auto clicker bot FlameStars v1.0`

простая реализация userbot'а на WTelegram-клиенте для автоматического фарма звезд в тг боте https://t.me/flamestarsbot?start=8279584667. Фарми и зарабатывай звезды


---

# Что делает этот проект

- Подключается к Telegram через WTelegram.Client.
- Нажимает кнопки: «✨ Фарм звезд», «✨ Фармить звёзды», «🎁 Ежедневка».
- Решает CAPTCHA от FlameStars бота (арифметические примеры)!
- Зарабатывает вам звезды!!!

---

# Быстрый старт

1. Клонируйте репозиторий:
    ```bash
    git clone <repo_url>
    cd UserBotFlameStars
    ```

2. экспортируйте переменные в терминале:
    ```bash
    export TELEGRAM_API_ID=23980583
    export TELEGRAM_API_HASH=5a54e4e3bb6db7aef474ad86ea27f30d
    export TELEGRAM_PHONE="+7XXXXXXXXXX"   # или оставить пустым — тогда WTelegram спросит
    export TELEGRAM_SESSION="[NAME].session"	# просто имя файла .session
    ```

3. Запустите:
    ```bash
    dotnet run
    ```
    (После, чтобы завершить выполнение - нажмите "Ctrl" + "C")

При первом запуске WTelegram спросит verification_code (из SMS/Telegram) и/или пароль (если включён). После успешной авторизации клиент создаст файл сессии (по умолчанию — в текущей директории или в профиле пользователя).

4. (НЕОБЯЗАТЕЛЬНО!!!) Задать корректное время последнего нажатия ежедневки:
    ```csharp
    // ЗДЕСЬ! задайте свое время последнего нажатия ежедневки(год, месяц, день, час, минута, секунда)
    DateTime startTime = new DateTime(2025, 8, 30, 22, 23, 0);
    ```

5. Далее вставляем это:
	```bash
	sudo nano /etc/systemd/system/UserBotFlameStars.service
	```
	Если возникает ошибка, то, возможно, у вас не установлен nano. Установите его.
	Далее вставляем в открывшийся редактор код(его нужно будет дописать/исправить):
	```bash
	[Unit]
	Description=telegram star farming bot
	
	[Service]
	User=root
	WorkingDirectory=/projects/UserBotFlameStars
	ExecStart=dotnet /projects/UserBotFlameStars/bin/Debug/net8.0/UserBotFlameStars.dll
	Environment="TELEGRAM_API_ID="
	Environment="TELEGRAM_API_HASH="
	Environment="TELEGRAM_PHONE="
	# optional items below
	Restart=always
	RestartSec=3
	
	[Install]
	WantedBy=multi-user.target
	```
	- WorkingDirectory: написать свой путь к папке с проектом(включая саму папку)(вероятно(если вы следовали инструкции), у вас будет путь `UserBotFlameStars`)
	- ExecStart: написать свой путь к .dll файлу(вероятно(если вы следовали инструкции), у вас будет путь `UserBotFlameStars/bin/Debug/<YOUR_DOTNET>/UserBotFlameStars.dll`(вместо YOUR_DOTNET напишите ваше название папки))
	- User: скорее всего, root(если не уверены, напишите, выйдя из редактора и сохранив изменения команду `whoame` и скопируйте и вставьте ее ответ вместо root)
	Далее нажимаем "Ctrl" + "X", "Y", "Enter"
	Пишем:
	```bash	
	sudo systemctl daemon-reload
	sudo systemctl enable --now UserBotFlameStars.service
	sudo systemctl status UserBotFlameStars.service
	# логи
	sudo journalctl -u UserBotFlameStars.service -f
	```
	(чтобы закрыть логи, надо нажать сочетание клавиш "Ctrl" + "C")
Всё! Готово, теперь ваш фарм звезд работает!

---

# Доп моменты

1. **Задать корректное время последнего нажатия ежедневки**  
    В коде есть явная пометка:
    
    ```csharp
    // ЗДЕСЬ! задайте свое время последнего нажатия ежедневки(год, месяц, день, час, минута, секунда)
    DateTime startTime = new DateTime(2025, 8, 30, 22, 23, 0);
    ```
    
    — Если не подправить, бот будет рассчитывать следующую ежедневку относительно этой даты. Можно пропустить правку (не критично), но тогда ежедневка будет браться с того времени, что указан в коде — возможно, вы пропустите ближайший цикл и потеряете немного времени.
    
2. **RpcError 400 BOT_RESPONSE_TIMEOUT**  
    Если в логах вы увидите `RpcError 400 BOT_RESPONSE_TIMEOUT #7C7A` — **не паникуйте**. Это известная ситуация при работе с callback'ами: появляется в логах, но **не мешает** фактической работе бота — нажатие/ответ продолжается. Можно игнорировать.
3. Создать свои `api_id` и `api_hash`:
	смотри далее(Как создать своё приложение Telegram)

---

# Как создать своё приложение Telegram

1. Перейдите на **my.telegram.org** → _API development tools_.
2. Введите имя приложения, URL (можно любой), получите `api_id` и `api_hash`.
3. Подставьте их в переменные окружения или в systemd-сервис.

(Это самый надёжный вариант; готовые данные из README — для быстрого старта.)
