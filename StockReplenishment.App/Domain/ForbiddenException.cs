namespace StockReplenishment.App.Domain;

public class ForbiddenException(string message) : Exception(message);
