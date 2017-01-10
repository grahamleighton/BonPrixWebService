namespace BonPrixWebService.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;

    public class ResponseModel
    {
        // Your context has been configured to use a 'ReponseModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'BonPrixWebService.Models.ReponseModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'ReponseModel' 
        // connection string in the application configuration file.
        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public string Time { get; set; }
        public string Result { get; set; }
        public List<string> Information = new List<string>();
        public List<string> Errors = new List<string>();
    }
}