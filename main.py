file_name = "玩家名称.oldxia"

with open(file_name, "r", encoding="utf-8") as f:
    lines = f.readlines()

cleaned_lines = []

for line in lines:
    line = line.strip()
    
    if not line:
        continue

    if "、" in line:
        name = line.split("、", 1)[1].strip()
        cleaned_lines.append(name)
    else:
        cleaned_lines.append(line)
with open(file_name, "w", encoding="utf-8") as f:
    f.write("\n".join(cleaned_lines))

print(f"✅ 一共清理了 {len(cleaned_lines)} 个玩家名字")