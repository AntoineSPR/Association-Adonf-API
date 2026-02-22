using AssociationAdonfAPI.Models;

public class Mail
{
    public string? MailTo { get; set; }
    public string? MailSubject { get; set; }
    public string? MailBody { get; set; }
    public string? MailFrom { get; set; }
    public UserApp? Receiver { get; set; }
}