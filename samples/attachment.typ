#set page(margin: 2cm)
#set text(font: "更纱黑体 UI SC", size: 12pt)

= {{ user.name }} 的附件

#table(
  columns: 2,
  [姓名], [{{ user.name }}],
  [事项], [{{ user.title }}],
)
