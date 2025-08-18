import os
import re

script_dir = os.path.dirname(os.path.abspath(__file__))

# Input and output file paths
input_file = os.path.join(script_dir, '..', 'hotelListHtml.txt')
output_file = os.path.join(script_dir, '..', 'hotelListCleaned.txt')

with open(input_file, "r", encoding="utf-8") as f:
    content = f.read()

# Split on </li> in case it's all one line
items = re.split(r"</li>", content)

cleaned_lines = []
for item in items:
    item = item.strip()
    if not item:
        continue
    # Remove <li> tags
    item = re.sub(r"</?li>", "", item)
    # Remove everything starting from ") -"
    cleaned_line = re.sub(r"\)\s-\s.*", ")", item)
    cleaned_lines.append(cleaned_line)

with open(output_file, "w", encoding="utf-8") as f:
    f.write("\n".join(cleaned_lines))

print(f"Cleaned hotel names saved to: {output_file}")
