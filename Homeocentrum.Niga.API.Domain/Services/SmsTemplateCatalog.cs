namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Clinic SMS texts. Placeholders stay in the body until a send fills them.
/// Seeding these rows does not send a message.
/// </summary>
public static class SmsTemplateCatalog
{
    public static readonly (string Code, string Body)[] All =
    {
        ("OTP", "Your Homeocentrum verification code is {otp}. It is valid for {minutes} minutes. Do not share this code."),
        ("PAYOUT_OTP", "Homeocentrum payout approval code: {otp}. Amount {amount}. Valid for {minutes} minutes. Do not share this code."),
        ("APPT_CONFIRM", "Your Homeocentrum appointment with {doctor} is confirmed for {date} at {time}."),
        ("APPT_CANCEL", "Your appointment on {slot} was cancelled{reason}. Contact the clinic if you need a new slot."),
        ("APPT_RESCHEDULE", "Your appointment moved from {oldSlot} to {newSlot}."),
        ("APPT_REQUEST", "Your appointment request for {date} at {time} was received. The clinic will confirm the time."),
        ("APPT_PROPOSED", "The clinic proposed {date} at {time}. Open the app to accept or decline."),
        ("WAITLIST_OFFER", "Hi {name}, a slot opened on {when}. Open the app to book. This offer does not reserve the slot."),
        ("TELE_READY", "Your tele consultation is ready. Room {room}. Open the waiting room in the app to join."),
        ("FOLLOW_UP", "Your follow-up with {doctor} is due on {date}. Open the app to book."),
        ("PRESCRIPTION", "Your Homeocentrum prescription is ready. Open the app to view it."),
        ("MEDICINE_ORDER", "Your medicine order update: {status}. Open the app for details."),
        ("PAYMENT", "Payment of {amount} for your Homeocentrum visit is recorded."),
        ("REMINDER", "Reminder from Homeocentrum: {title}. Due {due}."),
        ("CHECK_IN", "You are checked in for today's visit with {doctor}. Status: {status}.")
    };
}
