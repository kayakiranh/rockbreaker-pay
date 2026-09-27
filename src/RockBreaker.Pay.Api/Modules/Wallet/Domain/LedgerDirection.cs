namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Ledger kaydının debit veya credit yönünü belirtir.
/// EN: Defines whether a ledger entry is debit or credit.
/// Architecture: Double-Entry Ledger.
/// </summary>
public enum LedgerDirection
{
    /// <summary>TR: Bakiyeyi azaltan kayıt. EN: Entry reducing balance. Architecture: Double-Entry Ledger.</summary>
    Debit = 1,
    /// <summary>TR: Bakiyeyi artıran kayıt. EN: Entry increasing balance. Architecture: Double-Entry Ledger.</summary>
    Credit = 2
}
