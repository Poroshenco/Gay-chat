var app = new Vue({
    el: '#main',


    data: {
        amountoffriends: 0,

        allchats: {},
        isChat: false,

        messages: {},
        draft: "",

        chatterId: "",
        chatterNickname: "",
        chatterFullName: ""
    },





    methods: {

        sendMessage: function () {

            if (this.draft !== "") {
                $.ajax({
                    url: '/Account/SendMessage',
                    type: 'GET',
                    data: {
                        userId: this.chatterId,
                        message: this.draft
                    },
                    success: function (data) {

                        var draftmes = { MessageInner: this.draft, FromMe: true, SendTime: new Date() };

                        this.messages.push(draftmes);

                        this.goToBottom();

                        this.draft = "";

                    }.bind(this)
                });
            }
        },

        goToChat: function (id, fullname, nickname) {

            if (this.isChat) {

                var div = document.getElementById("messages");
                $(div).animate({ opacity: 0 }, 200);

                window.history.pushState("", "", "/Account/UserChat?userID=" + id);

                if (this.chatterId !== id) {

                    this.chatterId = id;
                    this.chatterFullName = fullname;
                    this.chatterNickname = nickname.substring(1);

                    $.ajax({
                        url: '/Account/GetMessangesForChat',
                        type: 'GET',
                        data: {
                            userID: id,
                        },
                        success: function (data) {

                            this.messages = JSON.parse(data);

                            this.goToBottom();

                            $(div).animate({ opacity: 1 }, 200);

                        }.bind(this),
                    });
                }

            }
            else {

                window.location.href = "/Account/UserChat?userID=" + id;

            }

        },

        getAmountOfFriends: function () {

            $.ajax({
                url: '/Account/GetNumberOfFriends',
                type: 'GET',
                success: function (data) {

                    this.amountoffriends = data;

                }.bind(this)
            });
        },

        goToBottom: function () {
            setTimeout(function () {
                var element = document.getElementById("messages");
                element.scrollTop = element.scrollHeight;
            }, 120);
        }

    },





    computed: {

    },





    mounted() {

        var url = window.location.href;

        var isChat = url.indexOf("UserChat") > 0 ? true : false;
        this.isChat = isChat;
        var id = "";

        if (url.indexOf("UserChat") > 0 && url.indexOf("userID") > url.indexOf("UserChat")) {
            id = url.substring(url.indexOf("userID") + 7);
            this.chatterId = id;

            $.ajax({
                url: '/Account/GetMessangesForChat',
                type: 'GET',
                data: {
                    userID: id,
                },
                success: function (data) {

                    this.messages = JSON.parse(data);

                    this.goToBottom();

                }.bind(this),
            });

            setInterval(function () {

                $.ajax({
                    url: '/Account/GetNewMessages',
                    type: 'GET',
                    data: {
                        id: this.chatterId
                    },
                    success: function (data) {
                        if (data.length > 10) {
                            var messages = JSON.parse(data);

                            for (var i = 0; i < messages.length; i++) {
                                this.messages.push(messages[i]);
                            }

                            this.goToBottom();
                        }


                    }.bind(this)
                });

            }.bind(this), 500);
        }

        $.ajax({
            url: '/Account/IsAuthorized',
            type: 'GET',
            success: function (data) {

                if (data === "True") {
                    this.getAmountOfFriends();

                    $.ajax({
                        url: '/Account/GetChats',
                        type: 'GET',
                        success: function (data) {

                            var allchats = JSON.parse(data);
                            this.allchats = allchats;

                            if (isChat) {
                                var user = allchats.filter(e => e.UserId === id)[0];
                                this.chatterFullName = user.UserFullname;
                                this.chatterNickname = user.UserNickname.substring(1);

                                $("#chatterInf").animate({ opacity: 1 }, 500);
                            }

                        }.bind(this)
                    });

                }


            }.bind(this)
        });






    }
});