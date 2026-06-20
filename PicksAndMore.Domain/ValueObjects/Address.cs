namespace PicksAndMore.Domain.ValueObjects;

public record Address(
    string Governorate, 
    string DetailedAddress, 
    string PrimaryPhone, 
    string? SecondaryPhone = null
);
