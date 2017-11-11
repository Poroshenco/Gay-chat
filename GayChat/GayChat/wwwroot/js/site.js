var app = new Vue({
    el: '#main',


    data: {
        amountoffriends: 0,

        chatterId: "",
        allchats: {},
        isChat: false,

        //myId: "",
        //myNickname: "",
        
        chatterNickname: "",
        chatterFullName: ""
    },





    methods: {
        
        goToChat: function (id, fullname, nickname) {

            if (this.isChat) {
                
                window.history.pushState("", "", "/Account/UserChat?userID=" + id);

                if (this.chatterId != id) {

                    this.chatterId = id;
                    this.chatterFullName = fullname;
                    this.chatterNickname = nickname.substring(1);
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

                            var user = allchats.filter(e => e.UserId === id)[0];
                            this.chatterFullName = user.UserFullname;
                            this.chatterNickname = user.UserNickname.substring(1);

                            $("#chatterInf").animate({ opacity: 1 }, 500);

                        }.bind(this)
                    });

                }

            }.bind(this)
        });


        

        

    }
});