use clap::Subcommand;
use tusktsk::TuskResult;

#[derive(Subcommand)]
pub enum ServicesCommand {
    Start,
    Stop,
    Status,
    Restart,
    Logs,
}

pub fn run(cmd: ServicesCommand) -> TuskResult<()> {
    match cmd {
        ServicesCommand::Start => { 
            println!("[services start] stub"); 
            Ok(()) 
        }
        ServicesCommand::Stop => { 
            println!("[services stop] stub"); 
            Ok(()) 
        }
        ServicesCommand::Status => { 
            println!("[services status] stub"); 
            Ok(()) 
        }
        ServicesCommand::Restart => { 
            println!("[services restart] stub"); 
            Ok(()) 
        }
        ServicesCommand::Logs => { 
            println!("[services logs] stub"); 
            Ok(()) 
        }
    }
} 