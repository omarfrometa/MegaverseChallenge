using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    /*****************************************/
    /* 
     * By:   Omar Frometa
     * For:  Crossmint 
     * Date: Oct 19, 2024 : 12:48PM
     * 
    /*****************************************/

    // API base URL for interacting with the Crossmint API challenge
    private const string BaseUrl = "https://challenge.crossmint.io/api";

    // Candidate ID, used to identify the user in the API requests
    private const string CandidateId = "0e27919e-d9b5-46a6-97ec-86aa0ee65ede";

    // API endpoint for retrieving the goal pattern from the server
    private const string GoalUrl = $"{BaseUrl}/map/{CandidateId}/goal";

    // Reusable HttpClient for sending requests. It avoids creating new instances for each request, improving performance.
    private static readonly HttpClient client = new HttpClient();

    /// <summary>
    /// The main entry point of the program. It triggers the process to create a pattern on the megaverse grid.
    /// </summary>
    static async Task Main(string[] args)
    {
        // Call the method to create a pattern in the megaverse by fetching the target goal pattern.
        await CreatePolyanetPattern();
    }

    /// <summary>
    /// Fetches the target goal pattern from the API and processes it to create the corresponding entities (polyanet, soloon, cometh).
    /// </summary>
    static async Task CreatePolyanetPattern()
    {
        try
        {
            // Send a GET request to the API endpoint to retrieve the goal pattern as a JSON response
            var response = await client.GetStringAsync(GoalUrl);

            // Parse the JSON response and retrieve the 'goal' array from the root JSON object
            var goalArray = JsonDocument.Parse(response).RootElement.GetProperty("goal").EnumerateArray();

            int row = 0; // Initialize the row index for grid traversal

            // Iterate through each row in the goal array (representing the megaverse grid)
            foreach (var rowElement in goalArray)
            {
                int column = 0; // Initialize the column index for grid traversal

                // Iterate through each cell in the current row
                foreach (var cell in rowElement.EnumerateArray())
                {
                    // Convert the cell content to lowercase for easier comparison
                    var cellValue = cell.ToString().ToLower();

                    // Skip cells that contain "space" as they are empty and should not have any entity placed
                    if (!cellValue.Contains("space"))
                    {
                        // Determine the entity type and any additional parameter (e.g., color for soloons, direction for comeths)
                        string entityType = GetEntityType(cellValue, out string? additionalParam);

                        // Call the method to create the entity on the grid
                        await CreateEntity(entityType, row, column, additionalParam);
                    }
                    column++; // Move to the next column
                }
                row++; // Move to the next row
            }
        }
        catch (Exception ex)
        {
            // If an exception occurs (e.g., network error), log it to the console
            Console.WriteLine($"Error obtaining goal coordinates: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the type of entity to create based on the content of the cell.
    /// It also extracts any additional parameters, such as color or direction, if applicable.
    /// </summary>
    /// <param name="cellValue">The content of the cell (e.g., "polyanet", "red_soloon").</param>
    /// <param name="additionalParam">An optional output parameter to capture extra data (e.g., color for soloons).</param>
    /// <returns>A string representing the entity type ("polyanets", "soloons", "comeths").</returns>
    static string GetEntityType(string cellValue, out string? additionalParam)
    {
        additionalParam = null; // Initialize the additional parameter to null

        // Check if the cell contains a polyanet
        if (cellValue.Contains("polyanet"))
            return "polyanets";

        // Check if the cell contains a soloon (and extract the color)
        if (cellValue.Contains("soloon"))
        {
            additionalParam = cellValue.Split('_')[0]; // Extract the color from the cell (e.g., "red" from "red_soloon")
            return "soloons";
        }

        // Check if the cell contains a cometh (and extract the direction)
        if (cellValue.Contains("cometh"))
        {
            additionalParam = cellValue.Split('_')[0]; // Extract the direction (e.g., "left" from "left_cometh")
            return "comeths";
        }

        // If no entity is found, return an empty string
        return string.Empty;
    }

    /// <summary>
    /// Creates an entity at the specified row and column in the grid.
    /// This method sends a POST request to the API to place the entity on the megaverse.
    /// </summary>
    /// <param name="entity">The type of entity to create (e.g., "polyanets", "soloons", "comeths").</param>
    /// <param name="row">The row in the grid where the entity should be placed.</param>
    /// <param name="column">The column in the grid where the entity should be placed.</param>
    /// <param name="param">An optional parameter, such as the color for soloons or direction for comeths.</param>
    static async Task CreateEntity(string entity, int row, int column, string? param = null)
    {
        // Construct the payload with the candidate ID, row, column, and any additional parameter
        var payload = new
        {
            candidateId = CandidateId,
            row,
            column,
            additionalParam = param
        };

        // Send the POST request to create the entity using a common request-sending method
        await SendRequest(HttpMethod.Post, entity, payload);
    }

    /// <summary>
    /// A generic method to send HTTP requests to the API, either to create (POST) or delete (DELETE) an entity.
    /// </summary>
    /// <param name="method">The HTTP method to use (e.g., POST or DELETE).</param>
    /// <param name="entity">The type of entity being manipulated (e.g., "polyanets", "soloons").</param>
    /// <param name="payload">The data to be sent in the request body, containing entity details.</param>
    static async Task SendRequest(HttpMethod method, string entity, object payload)
    {
        // Construct the full API URL for the entity type
        var url = $"{BaseUrl}/{entity}";

        // Create an HTTP request with the specified method (POST or DELETE) and attach the payload as JSON content
        var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        try
        {
            // Send the request and get the response
            var response = await client.SendAsync(request);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(method == HttpMethod.Post
                    ? $"Entity created at ({payload})" // Log successful creation
                    : $"Entity deleted at ({payload})"); // Log successful deletion
            }
            else
            {
                // Log any errors encountered during the request
                Console.WriteLine($"Error with {method} entity at ({payload}): {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the HTTP request
            Console.WriteLine($"Exception during {method} entity at ({payload}): {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans the entire megaverse by deleting all entities from the grid.
    /// This method sends multiple DELETE requests for each cell in the grid.
    /// </summary>
    static async Task CleanMegaverse()
    {
        const int size = 30; // Define the size of the megaverse grid (30x30)
        var tasks = new List<Task>(); // List to store all deletion tasks

        // Iterate over each cell in the grid
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                // Add deletion tasks for all entity types (polyanets, soloons, comeths) in each cell
                tasks.AddRange(new[]
                {
                    DeleteEntity("polyanets", row, column),
                    DeleteEntity("soloons", row, column),
                    DeleteEntity("comeths", row, column)
                });
            }
        }

        // Wait for all deletion tasks to complete
        await Task.WhenAll(tasks);
        Console.WriteLine("Megaverse completely cleaned."); // Log completion of the cleaning process
    }

    /// <summary>
    /// Deletes an entity at the specified row and column in the grid.
    /// This method sends a DELETE request to the API to remove the entity.
    /// </summary>
    /// <param name="entity">The type of entity to delete (e.g., "polyanets", "soloons", "comeths").</param>
    /// <param name="row">The row in the grid where the entity is located.</param>
    /// <param name="column">The column in the grid where the entity is located.</param>
    /// <param name="param">An optional parameter, such as the color for soloons or direction for comeths.</param>
    static async Task DeleteEntity(string entity, int row, int column, string? param = null)
    {
        // Construct the payload with the candidate ID, row, column, and any additional parameter
        var payload = new
        {
            candidateId = CandidateId,
            row,
            column,
            additionalParam = param
        };

        // Send the DELETE request to remove the entity using the common request-sending method
        await SendRequest(HttpMethod.Delete, entity, payload);
    }
}
