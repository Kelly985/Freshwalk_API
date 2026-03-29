namespace Freshwalk.Domain;

public enum UserRole
{
    Customer = 1,
    Agent = 2,
    Rider = 3,
    Admin = 4
}

public enum ShoeType
{
    Sneakers = 1,
    Suede = 2,
    Nubuck = 3,
    OfficialLeather = 4
}

public enum ColorTier
{
    BlackDark = 1,
    MixedColored = 2,
    WhiteLight = 3
}

public enum AddOnType
{
    Waterproofing = 1,
    ShadeChanging = 2,
    PickupDelivery = 3,
    ExpressService = 4
}

public enum BookingStatus
{
    PendingPayment = 1,
    PaymentConfirmed = 2,
    Cancelled = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Initiated = 2,
    Success = 3,
    Failed = 4
}

public enum OrderStatus
{
    PaymentConfirmed = 1,
    AwaitingPickupRider = 2,
    PickupRiderAssigned = 3,
    PickupOtpVerified = 4,
    InTransitToShop = 5,
    AtShop = 6,
    CleaningInProgress = 7,
    CleaningComplete = 8,
    AwaitingDeliveryRider = 9,
    DeliveryRiderAssigned = 10,
    InTransitToCustomer = 11,
    DeliveryOtpVerified = 12,
    Delivered = 13
}

public enum AssignmentType
{
    Pickup = 1,
    Delivery = 2
}

public enum AssignmentStatus
{
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}
