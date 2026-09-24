#set page(
  paper: "a4",
  margin: (x: 2.5cm, y: 3.5cm),
  background: [
    #set image(width: 60%)
    #image("Assets/rp-logo-bg.jpg")
  ]
)

#set text(
  font: ("更纱黑体 UI SC", "Times New Roman", "SimSun", "Songti SC", "Noto Serif CJK SC", "STSong"),
  size: 12pt,
  lang: "zh"
)

#set par(
  justify: true,
  first-line-indent: 2em,
  leading: 1.2em
)

// Header Section
#grid(
  columns: (auto, 1fr),
  align: (left, right),
  [
    // Top Logo
    #image("Assets/rp-logo-title.jpg", height: 2cm)
  ],
  [
    #set text(size: 9pt, fill: luma(60))
    #set par(first-line-indent: 0em, leading: 0.6em)
    #align(right)[
      浅圳市北山区北海大道3688号 \
      浅圳大学魔法与超能工程学院S305 \
      Email: {{ mail }} \
      电话: {{ phone }} \
      负责人: {{ name }}
    ]
  ]
)

#v(0.2cm)
#line(length: 100%, stroke: 0.5pt + luma(120))
#v(1.5cm)

// Title
#align(center)[
  #text(size: 22pt, weight: "bold", font: ("SimHei", "Heiti SC", "Noto Sans CJK SC", "STHeiti"))[
    笔试面试结果通知书
  ]
]

#v(1.5cm)

// Greeting
#par(first-line-indent: 0em)[
  #text(weight: "bold", size: 14pt)[亲爱的{{ user.name }}同学：]
]

#set par(leading: 0.5em)

{{ if user.admitted == 'true' }}
{{ user.name }}你好，恭喜你成功通过了RobotPilots *{{ user.group }}* 招新的笔试和面试，正式加入了RobotPilots！获得参与后续培训的资格。

  {{ case user.group }}
    {{- when '电控组','硬件组' }}
在交流过程中，我们看到了你的热情和向往，不论是逻辑思维还是对技术问题的探索态度都与我们的发展方向高度契合。
    {{- when '机械组' }}
在交流过程中，你展现出的思维和结构认知令我们印象深刻，也希望你保持热情和严谨的工作态度，完成修改、打磨、实测迭代等一系列的常态工作，在一次次实践中积累经验。
    {{- when '算法组' }}
在交流过程中，你体现出良好的专业能力以及对算法方向的向往。算法组聚焦自瞄、网络、导航、软件开发，需要兼顾理论和实物调试，期待你保持思辨与钻研，和团队互相协作！
    {{- when '运维组' }}
在交流过程中，我们看到了你严谨细致的处事方式和责任心，运维是社团运转的坚实后盾，起到了联动和支撑作用，也希望你能在集体贡献一份属于自己的力量!
    {{- else }}
出错! {{ user.group }}有点神秘了.
  {{ end }}

在未来的时间里期待你与组内的伙伴协作同行，在各类项目实践中将构想转化为真实，共同完成项目，并肩成长。

入组后请留意群通知，按时参加组内培训和项目任务，期待见面！
{{ else }}
{{ user.name }}你好，很感谢你对RobotPilots的热爱和喜欢！但是经过对笔试和面试结果的综合考量，很抱歉你与我们的需求目标有所偏差，未通过此次的{{ user.group }}笔试和面试。

此次结果并非对你个人能力的否定，我们真切看到了你的思考和热忱，也十分欣赏你所展现出来的闪光点，不要灰心！热爱不局限于一纸结果，在今后的学习生涯里我们相信你能在属于自己的舞台上绽放光芒，发挥自己的能力和才华。

再次感谢你为RobotPilots作出的准备和付出的努力，如有和你更契合的社团，我们也会与你联系！

祝愿你继续发光发热，后会有期。
{{ end }}

#v(3cm)

// Sign-off
#align(right)[
  #set par(first-line-indent: 0em)
  #text(size: 13pt, weight: "bold")[RobotPilots 战队] \
  #v(0.3em)
  {{ date }}
]


