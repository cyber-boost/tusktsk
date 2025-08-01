use clap::Subcommand;
use tusktsk::TuskResult;
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum LicenseCommand {
    Check,
    Generate { type_: String },
    Validate { file: String },
    Info { license: String },
}

pub fn run(cmd: LicenseCommand) -> TuskResult<()> {
    match cmd {
        LicenseCommand::Check => { 
            println!("[license check] stub"); 
            Ok(()) 
        }
        LicenseCommand::Generate { type_ } => { 
            println!("[license generate {}] stub", type_); 
            Ok(()) 
        }
        LicenseCommand::Validate { file } => { 
            println!("[license validate {}] stub", file); 
            Ok(()) 
        }
        LicenseCommand::Info { license } => { 
            println!("[license info {}] stub", license); 
            Ok(()) 
        }
    }
}

/// Generate a license file
fn license_generate(license_type: &str, author: Option<&str>, year: Option<&str>) -> TuskResult<()> {
    let current_year = if let Some(year) = year {
        year.to_string()
    } else {
        chrono::Utc::now().format("%Y").to_string()
    };
    let author_name = author.unwrap_or("C3B2");
    
    let license_content = match license_type.to_lowercase().as_str() {
        "mit" => format!(
            "MIT License

Copyright (c) {} {}

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the \"Software\"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.",
            current_year, author_name
        ),
        "apache" => format!(
            "Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   \"License\" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   \"Licensor\" shall mean the copyright owner or entity granting the License.

   \"Legal Entity\" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   \"control\" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   \"You\" (or \"Your\") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   \"Source\" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   \"Object\" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   \"Work\" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (which shall not include communications that are clearly marked or
   otherwise designated in writing by the copyright owner as \"Not a Contribution\").

   \"Contribution\" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, \"submitted\"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as \"Not a Contribution.\"

   \"Contributor\" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to use, reproduce, modify, display, perform,
   sublicense, and distribute the Work and such Derivative Works in
   Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, trademark, patent,
       and attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a \"NOTICE\" file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright notice to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an \"AS IS\" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Support. You may choose to offer,
   and to charge a fee for, warranty, support, indemnity or other
   liability obligations and/or rights consistent with this License.
   However, in accepting such obligations, You may act only on Your
   own behalf and on Your sole responsibility, not on behalf of any
   other Contributor, and only if You agree to indemnify, defend,
   and hold each Contributor harmless for any liability incurred by,
   or claims asserted against, such Contributor by reason of your
   accepting any such warranty or additional support.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets \"[]\"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same page as the copyright notice for easier identification within
   third-party archives.

Copyright {} {}

Licensed under the Apache License, Version 2.0 (the \"License\");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an \"AS IS\" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.",
            current_year, author_name
        ),
        "gpl" => format!(
            "GNU GENERAL PUBLIC LICENSE
Version 3, 29 June 2007

Copyright (C) {} {}

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.",
            current_year, author_name
        ),
        _ => {
            eprintln!("❌ Unknown license type: {}", license_type);
            eprintln!("Available types: mit, apache, gpl");
            std::process::exit(1);
        }
    };
    
    fs::write("LICENSE", license_content)?;
    println!("✅ {} license generated in LICENSE file", license_type.to_uppercase());
    
    Ok(())
}

/// Validate a license file
fn license_validate(file: &str) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ License file '{}' not found", file);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(file)?;
    
    // Basic license validation
    let content_lower = content.to_lowercase();
    let mut license_type = "unknown";
    
    if content_lower.contains("mit license") {
        license_type = "MIT";
    } else if content_lower.contains("apache license") {
        license_type = "Apache";
    } else if content_lower.contains("gnu general public license") {
        license_type = "GPL";
    }
    
    println!("✅ License file '{}' appears to be {}", file, license_type);
    
    Ok(())
}

/// Check for license files in a directory
fn license_check(path: Option<&str>) -> TuskResult<()> {
    let search_path = path.unwrap_or(".");
    let license_files = ["LICENSE", "LICENSE.txt", "license", "license.txt"];
    
    let mut found = false;
    for license_file in &license_files {
        let license_path = Path::new(search_path).join(license_file);
        if license_path.exists() {
            println!("✅ Found license file: {}", license_path.display());
            found = true;
        }
    }
    
    if !found {
        eprintln!("⚠️  No license file found in '{}'", search_path);
        std::process::exit(1);
    }
    
    Ok(())
}

/// Add license header to a file
fn license_add(license_type: &str, file: Option<&str>) -> TuskResult<()> {
    let target_file = file.unwrap_or("main.rs");
    
    if !Path::new(target_file).exists() {
        eprintln!("❌ File '{}' not found", target_file);
        std::process::exit(3);
    }
    
    let current_year = chrono::Utc::now().format("%Y").to_string();
    let author_name = "C3B2";
    
    let header = match license_type.to_lowercase().as_str() {
        "mit" => format!(
            "// MIT License
//
// Copyright (c) {} {}
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the \"Software\"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

",
            current_year, author_name
        ),
        _ => {
            eprintln!("❌ License type '{}' not supported for headers", license_type);
            std::process::exit(1);
        }
    };
    
    let content = fs::read_to_string(target_file)?;
    let new_content = format!("{}{}", header, content);
    fs::write(target_file, new_content)?;
    
    println!("✅ Added {} license header to '{}'", license_type.to_uppercase(), target_file);
    
    Ok(())
}

/// Remove license header from a file
fn license_remove(file: &str) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ File '{}' not found", file);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(file)?;
    let lines: Vec<&str> = content.lines().collect();
    
    // Find where license header ends
    let mut start_index = 0;
    for (i, line) in lines.iter().enumerate() {
        if line.trim().starts_with("//") && (line.contains("License") || line.contains("Copyright")) {
            start_index = i;
        } else if line.trim().is_empty() && start_index > 0 {
            start_index = i + 1;
            break;
        }
    }
    
    let new_content = lines[start_index..].join("\n");
    fs::write(file, new_content)?;
    
    println!("✅ Removed license header from '{}'", file);
    
    Ok(())
}

/// List available license types
fn license_list() -> TuskResult<()> {
    println!("📋 Available license types:");
    println!("  mit     - MIT License (permissive)");
    println!("  apache  - Apache License 2.0 (permissive)");
    println!("  gpl     - GNU General Public License v3 (copyleft)");
    println!("  bsd     - BSD License (permissive)");
    println!("  isc     - ISC License (permissive)");
    println!("  cc0     - Creative Commons Zero (public domain)");
    
    Ok(())
}

/// Get information about a license type
fn license_info(license_type: &str) -> TuskResult<()> {
    match license_type.to_lowercase().as_str() {
        "mit" => {
            println!("📄 MIT License");
            println!("   Type: Permissive");
            println!("   Allows: Commercial use, modification, distribution, private use");
            println!("   Requires: License and copyright notice");
            println!("   Prohibits: Liability and warranty");
            println!("   Compatible with: GPL, Apache, BSD");
        }
        "apache" => {
            println!("📄 Apache License 2.0");
            println!("   Type: Permissive");
            println!("   Allows: Commercial use, modification, distribution, patent use");
            println!("   Requires: License and copyright notice, state changes");
            println!("   Prohibits: Liability and warranty");
            println!("   Compatible with: GPL v3, MIT, BSD");
        }
        "gpl" => {
            println!("📄 GNU General Public License v3");
            println!("   Type: Copyleft");
            println!("   Allows: Commercial use, modification, distribution");
            println!("   Requires: License and copyright notice, source code disclosure");
            println!("   Prohibits: Liability and warranty");
            println!("   Compatible with: Apache 2.0, MIT");
        }
        _ => {
            eprintln!("❌ Unknown license type: {}", license_type);
            std::process::exit(1);
        }
    }
    
    Ok(())
} 