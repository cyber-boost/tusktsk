use clap::Subcommand;
use tusktsk::TuskResult;

#[derive(Subcommand)]
pub enum ConfigCommand {
    Show,
    Set { key: String, value: String },
    Get { key: String },
    Reset,
    Export { file: Option<String> },
    Import { file: String },
}

pub fn run(cmd: ConfigCommand) -> TuskResult<()> {
    match cmd {
        ConfigCommand::Show => { 
            println!("[config show] stub"); 
            Ok(()) 
        }
        ConfigCommand::Set { key, value } => { 
            println!("[config set {} {}] stub", key, value); 
            Ok(()) 
        }
        ConfigCommand::Get { key } => { 
            println!("[config get {}] stub", key); 
            Ok(()) 
        }
        ConfigCommand::Reset => { 
            println!("[config reset] stub"); 
            Ok(()) 
        }
        ConfigCommand::Export { file } => { 
            println!("[config export {:?}] stub", file); 
            Ok(()) 
        }
        ConfigCommand::Import { file } => { 
            println!("[config import {}] stub", file); 
            Ok(()) 
        }
    }
} 