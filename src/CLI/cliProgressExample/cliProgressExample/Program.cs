using cliProgressExample;
using static cliProgressExample.Repository<cliProgressExample.Ticket>;

internal class Program
{
    static async Task Main(string[] args)
    {
        using (var context = new ApplicationDbContext())
        {
            var unitOfWork = new TaketRepository(context);

            // Create
            var newTicket = new Ticket
            {
                Title = "Sample Ticket",
                Description = "This is a sample ticket",
                CreatedDate = DateTime.Now,
                Status = "Open"
            };
            await unitOfWork.AddAsync(newTicket);            

            Console.WriteLine("New ticket created.");

            // Read
            var ticket = await unitOfWork.GetByIdAsync(newTicket.Id);
            Console.WriteLine($"Ticket: {ticket.Title}, Description: {ticket.Description}");

            // Update
            ticket.Title = "Updated Title";
            await unitOfWork.UpdateAsync(ticket);


            Console.WriteLine("Ticket updated.");

            // Delete
            await unitOfWork.RemoveAsync(ticket);


            Console.WriteLine("Ticket deleted.");
        }
    }
}