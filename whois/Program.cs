﻿using System.Diagnostics;
using System.Net.Sockets;
// using whois;

Dictionary<String, User> DataBase = new Dictionary<String, User>
{
    {"cssbct",
      new User {UserID="cssbct",Surname="Imade",Fornames="Osarenoma",Title="student",
        Position="Student of Computer Science",
        Phone="+441482465222",Email="osaredanmail@gmail.com",Location="in RB-336" }
    },

};
Boolean debug = true;

if (args.Length == 0)
{
    Console.WriteLine("Starting Server");
    RunServer();
}
else
{
    for (int i = 0; i < args.Length; i++)
    {
        ProcessCommand(args[i]);
    }
}

void doRequest(NetworkStream socketStream)
{
    StreamWriter sw = new StreamWriter(socketStream);
    StreamReader sr = new StreamReader(socketStream);



    try
    {


        if (debug) Console.WriteLine("Waiting for input from client...");
        String line = sr.ReadLine();
        Console.WriteLine($"Received Network Command: '{line}'");


        //sw.WriteLine(line);   // Need to remove this line after testing
        //sw.Flush();           // Need to remove this line after testing   

        if (line == null)
        {
            if (debug) Console.WriteLine("Ignoring null command");
            return;
        }

        

        if (line == "POST / HTTP/1.1")
        {
            // The we have an update
            if (debug) Console.WriteLine("Received an update request");
            int content_length = 0;
            while (line != "")
            {
                if (line.StartsWith("Content-Length: "))
                {
                    content_length = Int32.Parse(line.Substring(16));
                }
                line = sr.ReadLine();
                if (debug) Console.WriteLine($"Skipped Header Line: '{line}'");
            }
            line = "";
            for (int i = 0; i < content_length; i++) line += (char)sr.Read();

            String[] slices = line.Split(new char[] { '&' }, 2);
            String ID = slices[0].Substring(5);
            String value = slices[1].Substring(9);
            // if (debug) Console.WriteLine($"Received an update request for '{ID}' to '{value}'");

            if (debug) Console.WriteLine($"Received an update request for '{ID}' to '{value}'");
            if (!DataBase.ContainsKey(ID)) DataBase.TryAdd(ID, new User { });
            DataBase[ID].Location = value;
        }
        else if (line.StartsWith("GET /?name=") && line.EndsWith(" HTTP/1.1"))
        {
            // then we have a lookup
            if (debug) Console.WriteLine("Received a lookup request");

            String[] slices = line.Split(" ");  // Split into 3 pieces
            String ID = slices[1].Substring(7);  // start at the 7th letter of the middle slice - skip `/?name=`


            if (DataBase.ContainsKey(ID))
            {
               
                string result = DataBase[ID].Location;
                sw.WriteLine("HTTP/1.1 200 OK");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.WriteLine(result);
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '{result}'");

            }
            else
            {
                sw.WriteLine("HTTP/1.1 404 Not Found");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '404 Not Found'");

            }

        }
        else
        {
            // We have an error
            Console.WriteLine($"Unrecognised command: '{line}'");


        }
    } catch (Exception e)
    {
        Console.WriteLine($"Fault in Command Processing: {e.ToString()}");
    }
    finally
    {
        sw.Close();
        sr.Close();
    }



}

/// Functions to process database requests
void Delete(String ID)
{
    if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");
    DataBase.Remove(ID);

}

void Update(String ID, String field, String update)
{
    if (debug)
        Console.WriteLine($" update field '{field}' to '{update}'");
    if (!DataBase.ContainsKey(ID)) DataBase.TryAdd(ID, new User { });
}

    void RunServer()
{
    TcpListener listener;
    Socket connection;
    NetworkStream socketStream;
    try
    {
        listener = new TcpListener(43);
        listener.Start();

        while (true)
        {
            if (debug) Console.WriteLine("Server Waiting connection...");
            connection = listener.AcceptSocket();
            connection.SendTimeout = 1000;
            connection.ReceiveTimeout = 1000;
            socketStream = new NetworkStream(connection);
            doRequest(socketStream);
            socketStream.Close();
            connection.Close();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
    if (debug)
        Console.WriteLine("Terminating Server");
}
/// Process the next database command request
void ProcessCommand(string command)
{
    if (debug) Console.WriteLine($"\nCommand: {command}");
    try
    {
        String[] slice = command.Split(new char[] { '?' }, 2);
        String ID = slice[0];
        String operation = null;
        String update = null;
        String field = null;
        if (slice.Length == 2)
        {
            operation = slice[1];
            if (operation == "")
            {
                // Is a record delete command
                Delete(ID);
                return;
            }
                String[] pieces = operation.Split(new char[] { '=' }, 2);
            field = pieces[0];
            if (pieces.Length == 2) update = pieces[1];
        }
        if (debug) Console.Write($"Operation on ID '{ID}'");
        if (operation == null ||
            update == null &&
            (!DataBase.ContainsKey(ID)))
        {
            Console.WriteLine($"User '{ID}' not known");
            return;
        }
        if (operation == null) Dump(ID);
        else if (update == null) Lookup(ID, field);
        else Update(ID, field, update);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Fault in Command Processing: {e.ToString()}");
    }

    /// Functions to process database requests
    void Dump(String ID)
    {
        if (debug) Console.WriteLine(" output all fields");
        Console.WriteLine($"UserID={DataBase[ID].UserID}");
        Console.WriteLine($"Surname={DataBase[ID].Surname}");
        Console.WriteLine($"Fornames={DataBase[ID].Fornames}");
        Console.WriteLine($"Title={DataBase[ID].Title}");
        Console.WriteLine($"Position={DataBase[ID].Position}");
        Console.WriteLine($"Phone={DataBase[ID].Phone}");
        Console.WriteLine($"Email={DataBase[ID].Email}");
        Console.WriteLine($"location={DataBase[ID].Location}");
    }
    void Lookup(String ID, String field)
    {
        if (debug)
            Console.WriteLine($" lookup field '{field}'");
        String result = null;
        switch (field)
        {
            case "location": result = DataBase[ID].Location; break;
            case "UserID": result = DataBase[ID].UserID; break;
            case "Surname": result = DataBase[ID].Surname; break;
            case "Fornames": result = DataBase[ID].Fornames; break;
            case "Title": result = DataBase[ID].Title; break;
            case "Phone": result = DataBase[ID].Phone; break;
            case "Position": result = DataBase[ID].Position; break;
            case "Email": result = DataBase[ID].Email; break;
        }
        Console.WriteLine(result);
    }
    void Update(String ID, String field, String update)
    {
        if (debug)
            Console.WriteLine($" update field '{field}' to '{update}'");
        switch (field)
        {
            case "location": DataBase[ID].Location = update; break;
            case "UserID": DataBase[ID].UserID = update; break;
            case "Surname": DataBase[ID].Surname = update; break;
            case "Fornames": DataBase[ID].Fornames = update; break;
            case "Title": DataBase[ID].Title = update; break;
            case "Phone": DataBase[ID].Phone = update; break;
            case "Position": DataBase[ID].Position = update; break;
            case "Email": DataBase[ID].Email = update; break;
        }
        Console.WriteLine("OK");
    }

}
