use clap::Subcommand;
use tusktsk::TuskResult;

#[derive(Subcommand)]
pub enum UtilityCommand {
    Parse { file: String },
    Format { file: String },
    Validate { file: String },
    Convert { input: String, output: String },
}

pub fn run(cmd: UtilityCommand) -> TuskResult<()> {
    match cmd {
        UtilityCommand::Parse { file } => { 
            println!("[utility parse {}] stub", file); 
            Ok(()) 
        }
        UtilityCommand::Format { file } => { 
            println!("[utility format {}] stub", file); 
            Ok(()) 
        }
        UtilityCommand::Validate { file } => { 
            println!("[utility validate {}] stub", file); 
            Ok(()) 
        }
        UtilityCommand::Convert { input, output } => { 
            println!("[utility convert {} {}] stub", input, output); 
            Ok(()) 
        }
    }
} 