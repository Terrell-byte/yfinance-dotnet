using System;

namespace YFinance.Yahoo.Entities;

public record Article(int id, string? title, string? body, string? link, DateTimeOffset? publishedAt);

