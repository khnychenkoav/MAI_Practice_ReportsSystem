# Проект "Sales PDF Report"

Проект "Sales PDF Report" представляет собой веб-приложение для генерации PDF отчета о продажах на основе данных из базы данных.

## Описание

Приложение разработано на ASP.NET Core с использованием Entity Framework Core для работы с базой данных. Оно предоставляет RESTful API для получения PDF отчета, содержащего информацию о продажах, общем доходе и топ-продавцах.

## Функциональность

- Генерация PDF отчета о продажах.
- Вывод общего дохода от продаж.
- Представление топ-продавцов по общему доходу.

## Технологии

1. ASP.NET Core

	- Microsoft.NET.Sdk.Web: Определяет проект как веб-приложение на платформе .NET.
	- TargetFramework: Целевая версия .NET - net8.0.
	- Nullable: Включает поддержку nullable reference types.
	- ImplicitUsings: Включает поддержку неявных using-директив.

2. Библиотеки и пакеты NuGet:

	- itext7 и itext7.bouncy-castle-adapter (обе версии 8.0.4): Библиотека для создания и редактирования PDF-документов.
	- Microsoft.AspNetCore.Authentication.Cookies (версия 2.2.0).
	- Microsoft.AspNetCore.Authentication.JwtBearer (версия 8.0.6).
	- Microsoft.AspNetCore.Identity (версия 2.2.0).
	- Microsoft.AspNetCore.Identity.EntityFrameworkCore (версия 8.0.6).
	- Microsoft.EntityFrameworkCore и Microsoft.EntityFrameworkCore.Design (обе версии 8.0.6).
	- Microsoft.EntityFrameworkCore.Tools (версия 8.0.6).
	- Microsoft.IdentityModel.Tokens (версия 7.6.2).
	- System.IdentityModel.Tokens.Jwt (версия 7.6.2).
	- NLog.Web (версия 5.3.11).
	- Npgsql.EntityFrameworkCore.PostgreSQL (версия 8.0.4).
	- SkiaSharp (версия 2.88.8).
	- Swashbuckle.AspNetCore (версия 6.6.2).

## Установка и настройка

### Требования

- .NET SDK 5.0 или выше
- PostgreSQL (или другая поддерживаемая база данных)

### Шаги установки

1. **Клонирование репозитория:**

2. **Настройка базы данных:**

Отредактируйте файл *appsettings.json* в разделе "ConnectionStrings" для соответствующей конфигурации вашей базы данных.

3. **Применение миграций:**

```bash
dotnet ef database update
```
### Запуск приложения
Для запуска приложения выполните следующие команды:

```bash
dotnet restore
dotnet run
```
После запуска риложение будет доступно по адресу *https://localhost:5001* (или другому порту, указанному в конфигурации).

## Дополнительная информация
Для получения дополнительной информации или помощи обратитесь к разработчику:

**Email:** khnartem@gmail.com

**GitHub:** github.com/khnychenkoav