namespace WarehouseAPI.DTOs.Analytics;

public class TurnoverDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public decimal IncomeAmount { get; set; }     // сумма всех приходов
    public decimal SaleAmount { get; set; }       // сумма всех продаж
    public decimal WriteOffAmount { get; set; }   // сумма всех списаний

    public decimal Profit => SaleAmount - IncomeAmount;  // грубая прибыль

    public List<TurnoverByDayDto> ByDay { get; set; } = new();  // разбивка по дням
}

public class TurnoverByDayDto
{
    public DateTime Date { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal SaleAmount { get; set; }
}