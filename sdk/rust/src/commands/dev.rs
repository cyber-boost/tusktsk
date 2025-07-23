use clap::Subcommand;
use tusktsk::TuskResult;

#[derive(Subcommand)]
pub enum DevCommand {
    Build,
    Watch,
    Serve,
    Test,
    Lint,
}

pub fn run(cmd: DevCommand) -> TuskResult<()> {
    match cmd {
        DevCommand::Build => { 
            println!("[dev build] stub"); 
            Ok(()) 
        }
        DevCommand::Watch => { 
            println!("[dev watch] stub"); 
            Ok(()) 
        }
        DevCommand::Serve => { 
            println!("[dev serve] stub"); 
            Ok(()) 
        }
        DevCommand::Test => { 
            println!("[dev test] stub"); 
            Ok(()) 
        }
        DevCommand::Lint => { 
            println!("[dev lint] stub"); 
            Ok(()) 
        }
    }
} 